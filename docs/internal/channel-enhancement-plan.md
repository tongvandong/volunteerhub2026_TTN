# Kế hoạch nâng cấp Kênh trao đổi sự kiện

Tài liệu này hướng dẫn từng bước thêm 8 tính năng cho channel, chia 3 mức: Dễ → Trung bình → Khó. Người mới cũng làm theo được.

## Tóm tắt

| Mức | Tính năng | Thời gian ước tính | Cần migration | Cần tool mới |
|---|---|---|---|---|
| Dễ 1 | Pin post | 30 phút | Có (1 cột) | Không |
| Dễ 2 | Post type (announcement/discussion/question) | 1 giờ | Có (1 cột) | Không |
| Dễ 3 | Reply comment (1 cấp) | 2 giờ | Có (1 cột) | Không |
| TB 4 | File attachment trong post | 3 giờ | Có (1 cột) | Không |
| TB 5 | Mention @user | 4 giờ | Có (1 bảng mới) | Không |
| TB 6 | Poll/vote | 4 giờ | Có (2 bảng mới) | Không |
| Khó 7 | Realtime chat | 1 ngày | Không | Có (SignalR) |
| Khó 8 | Sub-channel cho từng ca | 1 ngày | Có (1 cột) | Không |

**Tool cần cài thêm**:
- Mức Dễ + TB: KHÔNG cần gì thêm (đã có .NET 8, EF Core, React).
- Mức Khó 7 (Realtime): NuGet package `Microsoft.AspNetCore.SignalR` (đi kèm .NET — không cần cài thêm) + npm package `@microsoft/signalr` (cần `npm install`).

---

## Mức Dễ 1: Pin post

### Mục tiêu
Organizer có thể ghim 1-3 bài quan trọng (thông báo, lịch trình, nội quy) lên đầu kênh. Bài ghim luôn hiển thị trước, không bị đẩy xuống khi có bài mới.

### Bước 1: Sửa entity `Post.cs`

File: `BaseCore.Entities/Post.cs`

Thêm 2 dòng vào sau `LikeCount`:
```csharp
public bool IsPinned { get; set; } = false;
public DateTime? PinnedAt { get; set; }
```

### Bước 2: Tạo migration

```powershell
cd D:\FW\FW\BaseCore
dotnet ef migrations add AddPostPinning --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

Kiểm tra file migration vừa tạo trong `BaseCore.Repository/Migrations/` — chỉ thêm 2 cột, không có drop dữ liệu.

### Bước 3: Sửa `IChannelService.cs`

File: `BaseCore.Services/VolunteerHub/Engagement/IChannelService.cs`

Thêm 1 method:
```csharp
Task<Post> TogglePinAsync(int channelId, int postId, int organizerId, bool isAdmin);
```

### Bước 4: Sửa `ChannelService.cs`

Thêm logic vào cuối class:

```csharp
public async Task<Post> TogglePinAsync(int channelId, int postId, int organizerId, bool isAdmin)
{
    var channel = await _context.Channels.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == channelId)
        ?? throw new Exception("Channel not found");
    if (!isAdmin && channel.Event.OrganizerId != organizerId)
        throw new Exception("Only organizer or admin can pin posts");

    var post = await _context.Posts.FindAsync(postId) ?? throw new Exception("Post not found");
    if (post.ChannelId != channelId) throw new Exception("Post not in this channel");

    if (!post.IsPinned)
    {
        // Limit: max 3 pinned posts
        var pinnedCount = await _context.Posts.CountAsync(p => p.ChannelId == channelId && p.IsPinned);
        if (pinnedCount >= 3) throw new Exception("Maximum 3 pinned posts per channel");
    }

    post.IsPinned = !post.IsPinned;
    post.PinnedAt = post.IsPinned ? DateTime.UtcNow : null;
    await _context.SaveChangesAsync();
    return post;
}
```

Sửa `GetPostsAsync` để pinned post lên đầu:

```csharp
public async Task<(List<Post> Items, int TotalCount)> GetPostsAsync(int channelId, int page, int pageSize)
{
    var query = _context.Posts
        .Include(p => p.Author)
        .Where(p => p.ChannelId == channelId)
        .OrderByDescending(p => p.IsPinned)        // Pinned first
        .ThenByDescending(p => p.PinnedAt)         // Newest pin first
        .ThenByDescending(p => p.CreatedAt);       // Then by date

    var totalCount = await query.CountAsync();
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    return (items, totalCount);
}
```

### Bước 5: Sửa `ChannelsController.cs`

Thêm endpoint:

```csharp
[HttpPost("{id}/posts/{postId}/toggle-pin")]
public async Task<IActionResult> TogglePin(int id, int postId)
{
    var uid = GetUserId();
    if (uid == null) return Unauthorized();
    try
    {
        var post = await _channelService.TogglePinAsync(id, postId, uid.Value, IsAdmin());
        return Ok(post);
    }
    catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
}
```

### Bước 6: Sửa frontend `Channel.jsx`

Trong `PostCard` component, thêm UI:

```jsx
// Thêm vào dropdown menu của post (gần nút xóa)
{(channel?.event?.organizerId === currentUser?.id || currentUser?.role === 'Admin') && (
  <button onClick={() => onTogglePin(post.id)} className="text-gray-400 hover:text-yellow-500" title={post.isPinned ? 'Bỏ ghim' : 'Ghim'}>
    <i className={`fa-solid fa-thumbtack text-xs ${post.isPinned ? 'text-yellow-500' : ''}`} />
  </button>
)}

// Hiển thị badge "Đã ghim" trên đầu post nếu IsPinned
{post.isPinned && (
  <div className="px-4 py-1.5 bg-yellow-50 border-b border-yellow-100 text-xs text-yellow-700 flex items-center gap-1.5">
    <i className="fa-solid fa-thumbtack" /> Bài ghim
  </div>
)}
```

Trong `Channel` component thêm hàm:

```jsx
const handleTogglePin = async (postId) => {
  try {
    const r = await channelApi.togglePin(id, postId);
    setPosts(prev => prev.map(p => p.id === postId ? r.data : p)
                          .sort((a, b) => (b.isPinned - a.isPinned)));
  } catch (err) { alert(err.response?.data?.message || 'Lỗi'); }
};
```

### Bước 7: Sửa `api.js`

```javascript
togglePin: (channelId, postId) => api.post(`/channels/${channelId}/posts/${postId}/toggle-pin`),
```

### Bước 8: Build + test

```powershell
dotnet build BaseCore.sln
cd BaseCore.WebClient
npm run build
```

Test: Login organizer, vào channel của event mình, ghim 1 bài → kiểm tra hiển thị lên đầu.

### Tiêu chí đạt
- ☐ Build pass
- ☐ Organizer ghim/bỏ ghim bài được
- ☐ Volunteer thường KHÔNG có nút ghim
- ☐ Bài ghim hiển thị lên đầu, có badge "Bài ghim"
- ☐ Tối đa 3 bài ghim mỗi channel

---

## Mức Dễ 2: Post type (Phân loại bài viết)

### Mục tiêu
Mỗi post có 1 trong 3 loại: `announcement` (thông báo, có icon loa), `discussion` (thảo luận, mặc định), `question` (câu hỏi, có icon ?). User filter theo loại.

### Bước 1: Sửa `Post.cs`

```csharp
public string PostType { get; set; } = "discussion"; // announcement | discussion | question
```

### Bước 2: Migration

```powershell
dotnet ef migrations add AddPostType --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

Mở file migration vừa tạo, sửa `defaultValue: ""` thành `defaultValue: "discussion"` cho cột mới (để các post cũ có giá trị hợp lệ).

### Bước 3: Sửa `IChannelService` + `ChannelService`

`CreatePostAsync` nhận thêm `postType`:

```csharp
Task<Post> CreatePostAsync(int channelId, int authorId, string content, string? imageUrl, string postType);
```

Implementation:

```csharp
public async Task<Post> CreatePostAsync(int channelId, int authorId, string content, string? imageUrl, string postType)
{
    var validTypes = new[] { "announcement", "discussion", "question" };
    if (!validTypes.Contains(postType)) postType = "discussion";

    // Only organizer/admin can post announcement
    if (postType == "announcement")
    {
        var channel = await _context.Channels.Include(c => c.Event).FirstAsync(c => c.Id == channelId);
        var user = await _context.Users.FindAsync(authorId);
        if (channel.Event.OrganizerId != authorId && user?.UserType != 3)
            throw new Exception("Only organizer or admin can create announcements");
    }

    var post = new Post {
        ChannelId = channelId, AuthorId = authorId,
        Content = content, ImageUrl = imageUrl ?? "",
        PostType = postType,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };
    _context.Posts.Add(post);
    await _context.SaveChangesAsync();
    return post;
}
```

`GetPostsAsync` nhận thêm filter optional:

```csharp
public async Task<(List<Post> Items, int TotalCount)> GetPostsAsync(int channelId, int page, int pageSize, string? postType = null)
{
    var query = _context.Posts.Include(p => p.Author).Where(p => p.ChannelId == channelId);
    if (!string.IsNullOrEmpty(postType)) query = query.Where(p => p.PostType == postType);
    var ordered = query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt);
    // ... (giống cũ)
}
```

### Bước 4: Sửa `ChannelsController`

`CreatePost` DTO thêm `PostType`:

```csharp
public class PostCreateDto
{
    public string Content { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string PostType { get; set; } = "discussion";
}
```

`GetPosts` nhận query param:

```csharp
public async Task<IActionResult> GetPosts(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? type = null)
{
    // ...
    var (items, totalCount) = await _channelService.GetPostsAsync(id, page, pageSize, type);
    // ...
}
```

### Bước 5: Sửa frontend

Trong `Channel.jsx`, thêm state filter:

```jsx
const [filterType, setFilterType] = useState('all'); // all | announcement | discussion | question

// Khi gọi loadPosts
const r = await channelApi.getPosts(id, { 
  page: p, pageSize: 10,
  type: filterType === 'all' ? null : filterType 
});
```

Thêm UI filter tabs:

```jsx
<div className="card p-3 flex gap-2">
  {[
    { val: 'all', label: 'Tất cả', icon: 'fa-list' },
    { val: 'announcement', label: 'Thông báo', icon: 'fa-bullhorn' },
    { val: 'discussion', label: 'Thảo luận', icon: 'fa-comments' },
    { val: 'question', label: 'Câu hỏi', icon: 'fa-circle-question' }
  ].map(t => (
    <button key={t.val} onClick={() => { setFilterType(t.val); loadPosts(1); }}
      className={`btn-sm flex items-center gap-1.5 ${filterType === t.val ? 'btn-primary' : 'btn-secondary'}`}>
      <i className={`fa-solid ${t.icon}`} /> {t.label}
    </button>
  ))}
</div>
```

Trong `PostCard`, thêm badge type:

```jsx
const typeMap = {
  announcement: { label: 'Thông báo', color: 'bg-orange-100 text-orange-700', icon: 'fa-bullhorn' },
  question: { label: 'Câu hỏi', color: 'bg-blue-100 text-blue-700', icon: 'fa-circle-question' },
  discussion: null
};
const typeInfo = typeMap[post.postType];
// Render trước phần content:
{typeInfo && (
  <div className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${typeInfo.color} mb-2`}>
    <i className={`fa-solid ${typeInfo.icon}`} /> {typeInfo.label}
  </div>
)}
```

Trong form tạo post, thêm dropdown chọn type (chỉ organizer thấy "announcement"):

```jsx
<select value={postType} onChange={e => setPostType(e.target.value)} className="text-sm border rounded px-2 py-1">
  <option value="discussion">Thảo luận</option>
  <option value="question">Câu hỏi</option>
  {(channel?.event?.organizerId === user?.id || user?.role === 'Admin') && (
    <option value="announcement">Thông báo</option>
  )}
</select>
```

### Tiêu chí đạt
- ☐ Volunteer chỉ tạo được discussion + question
- ☐ Organizer/Admin tạo được cả 3 loại
- ☐ Filter tabs lọc đúng
- ☐ Badge hiển thị đúng màu/icon

---

## Mức Dễ 3: Reply comment (1 cấp)

### Mục tiêu
Comment có thể trả lời comment khác, hiển thị lùi vào (indent). Chỉ 1 cấp — reply của reply vẫn nằm cùng cấp.

### Bước 1: Sửa `Comment.cs`

```csharp
public int? ParentCommentId { get; set; }
public Comment ParentComment { get; set; }
```

### Bước 2: Sửa DbContext

File: `BaseCore.Repository/MySqlDbContext.cs`, trong `modelBuilder.Entity<Comment>`:

```csharp
entity.HasOne(e => e.ParentComment)
      .WithMany()
      .HasForeignKey(e => e.ParentCommentId)
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired(false);
```

### Bước 3: Migration

```powershell
dotnet ef migrations add AddCommentReply --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### Bước 4: Sửa service

```csharp
Task<Comment> AddCommentAsync(int channelId, int postId, int authorId, string content, int? parentCommentId);
```

Implementation thêm:
```csharp
if (parentCommentId.HasValue) {
    var parent = await _context.Comments.FindAsync(parentCommentId.Value);
    if (parent == null || parent.PostId != postId) throw new Exception("Parent comment invalid");
    if (parent.ParentCommentId.HasValue) throw new Exception("Reply nesting not allowed");
}
var comment = new Comment {
    PostId = postId, AuthorId = authorId, Content = content,
    ParentCommentId = parentCommentId,
    CreatedAt = DateTime.UtcNow
};
```

### Bước 5: Sửa controller

`CommentDto` thêm `ParentCommentId`:
```csharp
public class CommentDto {
    public string Content { get; set; } = "";
    public int? ParentCommentId { get; set; }
}
```

### Bước 6: Sửa frontend

Trong `CommentSection`:

```jsx
const [replyTo, setReplyTo] = useState(null); // commentId đang reply

const submit = async (e, parentId = null) => {
  // gọi addComment với parentCommentId: parentId
};

// Render comments:
const topLevel = comments.filter(c => !c.parentCommentId);
const replies = comments.filter(c => c.parentCommentId);

topLevel.map(c => (
  <div key={c.id}>
    {/* Render comment c */}
    <button onClick={() => setReplyTo(c.id)}>Trả lời</button>
    
    {/* Render replies cho comment này */}
    <div className="ml-9 mt-2 space-y-2">
      {replies.filter(r => r.parentCommentId === c.id).map(r => (
        <CommentItem key={r.id} comment={r} />
      ))}
      {replyTo === c.id && (
        <form onSubmit={e => submit(e, c.id)}>
          <input ... placeholder="Trả lời..." />
        </form>
      )}
    </div>
  </div>
))
```

### Tiêu chí đạt
- ☐ Click "Trả lời" hiện input lùi vào
- ☐ Reply chỉ 1 cấp (không cho reply của reply)
- ☐ Sort: comment cha cũ → mới, replies bên trong sort cũ → mới

---

## Mức Trung bình 4: File attachment trong post

### Mục tiêu
Post có thể đính kèm file (PDF, DOCX, ảnh thêm). User click tải về. Tận dụng `UploadsController` đã có.

### Bước 1: Sửa `Post.cs`

```csharp
public string AttachmentUrl { get; set; } = "";
public string AttachmentName { get; set; } = "";
public string AttachmentType { get; set; } = ""; // pdf | docx | image | other
public long AttachmentSize { get; set; } = 0; // bytes
```

### Bước 2: Migration

```powershell
dotnet ef migrations add AddPostAttachment --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### Bước 3: Mở rộng `UploadsController` để nhận file thường (nếu chưa)

Kiểm tra file `BaseCore.APIService/Controllers/Shared/UploadsController.cs`. Nếu chỉ nhận ảnh, thêm endpoint:

```csharp
[HttpPost("file")]
[Authorize]
public async Task<IActionResult> UploadFile(IFormFile file)
{
    if (file == null || file.Length == 0) return BadRequest(new { message = "File trống" });
    if (file.Length > 10 * 1024 * 1024) return BadRequest(new { message = "Tối đa 10MB" });
    
    var allowedExt = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".jpg", ".png", ".jpeg" };
    var ext = Path.GetExtension(file.FileName).ToLower();
    if (!allowedExt.Contains(ext)) return BadRequest(new { message = "Định dạng không hỗ trợ" });
    
    var fileName = $"{Guid.NewGuid():N}{ext}";
    var path = Path.Combine("wwwroot/uploads/files", fileName);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using (var stream = File.Create(path)) await file.CopyToAsync(stream);
    
    return Ok(new {
        url = $"/uploads/files/{fileName}",
        name = file.FileName,
        size = file.Length,
        type = ext.TrimStart('.')
    });
}
```

### Bước 4: Sửa service + controller

`CreatePostAsync` nhận thêm:
```csharp
Task<Post> CreatePostAsync(int channelId, int authorId, string content, string? imageUrl, string postType, AttachmentDto? attachment);
```

DTO:
```csharp
public class AttachmentDto {
    public string Url { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public long Size { get; set; }
}
```

### Bước 5: Frontend — component upload

Tạo component `FileUploadField.jsx`:

```jsx
import { useState } from 'react';
import api from '../../services/api';

export default function FileUploadField({ value, onChange, label = 'File đính kèm' }) {
  const [uploading, setUploading] = useState(false);
  
  const handleFile = async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    setUploading(true);
    try {
      const fd = new FormData();
      fd.append('file', file);
      const r = await api.post('/uploads/file', fd, { headers: { 'Content-Type': 'multipart/form-data' } });
      onChange(r.data);
    } catch (err) { alert(err.response?.data?.message || 'Upload thất bại'); }
    finally { setUploading(false); }
  };
  
  return (
    <div>
      <label className="text-xs text-gray-600">{label}</label>
      {!value?.url ? (
        <label className="block border-2 border-dashed border-gray-200 rounded-lg p-3 text-center cursor-pointer hover:border-primary-400">
          <input type="file" onChange={handleFile} className="hidden" disabled={uploading} />
          <i className="fa-solid fa-paperclip text-gray-400 mr-2" />
          {uploading ? 'Đang tải...' : 'Chọn file (PDF, DOCX, ảnh — tối đa 10MB)'}
        </label>
      ) : (
        <div className="flex items-center gap-2 p-2 bg-gray-50 rounded">
          <i className="fa-solid fa-file text-gray-500" />
          <span className="text-sm flex-1 truncate">{value.name}</span>
          <span className="text-xs text-gray-400">{(value.size / 1024).toFixed(0)} KB</span>
          <button onClick={() => onChange(null)} type="button" className="text-red-500"><i className="fa-solid fa-xmark" /></button>
        </div>
      )}
    </div>
  );
}
```

Trong PostCard render attachment:

```jsx
{post.attachmentUrl && (
  <a href={post.attachmentUrl} download={post.attachmentName} target="_blank" rel="noopener" 
     className="mt-2 flex items-center gap-2 p-2 bg-gray-50 rounded border hover:bg-gray-100">
    <i className={`fa-solid ${post.attachmentType === 'pdf' ? 'fa-file-pdf text-red-500' : 'fa-file text-gray-500'}`} />
    <span className="flex-1 text-sm">{post.attachmentName}</span>
    <span className="text-xs text-gray-400">{(post.attachmentSize / 1024).toFixed(0)} KB</span>
    <i className="fa-solid fa-download text-gray-400" />
  </a>
)}
```

### Tiêu chí đạt
- ☐ Upload PDF/DOCX/ảnh thành công, link ổn định
- ☐ Click tải file về máy
- ☐ Chặn file > 10MB hoặc không đúng định dạng

---

## Mức Trung bình 5: Mention @user

### Mục tiêu
Trong post/comment có thể tag `@username`. Người được tag nhận notification + tên hiển thị highlight xanh.

### Bước 1: Tạo entity `Mention.cs`

```csharp
public class Mention
{
    public int Id { get; set; }
    public string EntityType { get; set; } = ""; // Post | Comment
    public int EntityId { get; set; }
    public int MentionedUserId { get; set; }
    public int MentionerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User MentionedUser { get; set; }
    public User Mentioner { get; set; }
}
```

### Bước 2: Thêm DbSet + config

`MySqlDbContext.cs`:
```csharp
public DbSet<Mention> Mentions { get; set; }

modelBuilder.Entity<Mention>(entity => {
    entity.HasKey(e => e.Id);
    entity.Property(e => e.EntityType).HasMaxLength(20).IsRequired();
    entity.HasOne(e => e.MentionedUser).WithMany().HasForeignKey(e => e.MentionedUserId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.Mentioner).WithMany().HasForeignKey(e => e.MentionerUserId).OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(e => new { e.EntityType, e.EntityId });
    entity.HasIndex(e => e.MentionedUserId);
});
```

### Bước 3: Migration

```powershell
dotnet ef migrations add AddMentions --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### Bước 4: Logic parse mention

Trong `ChannelService.CreatePostAsync` và `AddCommentAsync`, sau khi save, parse content tìm `@username`:

```csharp
private async Task ProcessMentionsAsync(string content, string entityType, int entityId, int mentionerUserId, int channelId)
{
    var pattern = @"@(\w+)";
    var matches = System.Text.RegularExpressions.Regex.Matches(content, pattern);
    var usernames = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    if (usernames.Count == 0) return;
    
    var users = await _context.Users.Where(u => usernames.Contains(u.UserName)).ToListAsync();
    var canAccessUsers = new List<User>();
    foreach (var u in users) {
        if (await CanAccessChannelAsync(channelId, u.Id))
            canAccessUsers.Add(u);
    }
    
    foreach (var u in canAccessUsers) {
        _context.Mentions.Add(new Mention {
            EntityType = entityType, EntityId = entityId,
            MentionedUserId = u.Id, MentionerUserId = mentionerUserId,
            CreatedAt = DateTime.UtcNow
        });
        // TODO: gọi NotificationService gửi notification
    }
    await _context.SaveChangesAsync();
}
```

Inject `INotificationService` vào ChannelService và gọi:

```csharp
await _notificationService.SendAsync(u.Id, 
    "Bạn được nhắc đến", 
    $"{mentionerName} đã nhắc đến bạn trong một bài viết",
    "Mention", entityId);
```

### Bước 5: Frontend — autocomplete @

Trong textarea tạo post/comment, dùng thư viện đơn giản hoặc tự code:

Khi user gõ `@`, fetch danh sách user có quyền truy cập channel, hiện dropdown gợi ý.

Tạo endpoint helper:
```csharp
[HttpGet("{id}/members")]
public async Task<IActionResult> GetChannelMembers(int id, [FromQuery] string? query = null)
{
    // Trả về danh sách user (organizer + confirmed volunteers) match query
}
```

Frontend đơn giản:
```jsx
const [mentionQuery, setMentionQuery] = useState(null);
const [mentionResults, setMentionResults] = useState([]);

const handleContentChange = (e) => {
  const val = e.target.value;
  setContent(val);
  // Tìm @ ngay trước con trỏ
  const cursorPos = e.target.selectionStart;
  const beforeCursor = val.substring(0, cursorPos);
  const match = beforeCursor.match(/@(\w*)$/);
  if (match) {
    setMentionQuery(match[1]);
    channelApi.getMembers(channelId, match[1]).then(r => setMentionResults(r.data));
  } else {
    setMentionQuery(null);
  }
};
```

Render highlight `@username` thành link xanh khi hiển thị post:

```jsx
function renderContent(text) {
  return text.split(/(@\w+)/).map((part, i) => 
    part.startsWith('@') 
      ? <span key={i} className="text-primary-600 font-medium">{part}</span>
      : part
  );
}
```

### Tiêu chí đạt
- ☐ Gõ `@` hiện dropdown gợi ý user trong channel
- ☐ Sau khi chọn, mention được lưu vào DB
- ☐ Người được mention nhận notification
- ☐ Render `@username` highlight màu xanh

---

## Mức Trung bình 6: Poll/Vote

### Mục tiêu
Organizer tạo khảo sát trong channel: câu hỏi + 2-5 lựa chọn. Volunteer vote 1 lần. Hiển thị kết quả % real-time.

### Bước 1: Tạo entity

`Poll.cs`:
```csharp
public class Poll
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Question { get; set; } = "";
    public bool AllowMultiple { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Post Post { get; set; }
    public List<PollOption> Options { get; set; } = new();
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public string Text { get; set; } = "";
    public int VoteCount { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
    
    public Poll Poll { get; set; }
}

public class PollVote
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public int OptionId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Bước 2: DbContext + migration

Thêm 3 DbSet, config relationships, unique index `(PollId, UserId, OptionId)`.

```powershell
dotnet ef migrations add AddPolls --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### Bước 3: Service

```csharp
public interface IPollService
{
    Task<Poll> CreatePollAsync(int channelId, int postId, int organizerId, bool isAdmin, PollCreateDto dto);
    Task<Poll> VoteAsync(int channelId, int pollId, int optionId, int userId);
    Task<object> GetPollResultsAsync(int channelId, int pollId, int userId);
}
```

Implementation: validate organizer/admin, validate option thuộc poll, validate user chưa vote (nếu không AllowMultiple), tăng VoteCount, lưu PollVote.

### Bước 4: Controller endpoints

```csharp
POST   /api/channels/{id}/posts/{postId}/poll          [Organizer/Admin]
POST   /api/channels/{id}/polls/{pollId}/vote          [Authenticated]
GET    /api/channels/{id}/polls/{pollId}/results       [Authenticated]
```

### Bước 5: Frontend

Khi tạo post, có toggle "Đính kèm khảo sát":

```jsx
{showPollForm && (
  <div className="border rounded p-3 space-y-2">
    <input placeholder="Câu hỏi" value={question} onChange={e => setQuestion(e.target.value)} />
    {options.map((opt, i) => (
      <input key={i} placeholder={`Lựa chọn ${i+1}`} value={opt} 
             onChange={e => setOptions(prev => { const n = [...prev]; n[i] = e.target.value; return n; })} />
    ))}
    {options.length < 5 && (
      <button onClick={() => setOptions(prev => [...prev, ''])}>+ Thêm lựa chọn</button>
    )}
  </div>
)}
```

Render poll trong PostCard:

```jsx
{post.poll && (
  <div className="mt-3 border rounded p-3 bg-gray-50">
    <p className="font-medium">{post.poll.question}</p>
    <div className="space-y-2 mt-2">
      {post.poll.options.map(opt => {
        const total = post.poll.options.reduce((s, o) => s + o.voteCount, 0);
        const percent = total > 0 ? (opt.voteCount / total * 100).toFixed(0) : 0;
        const voted = post.poll.userVotedOptionId === opt.id;
        return (
          <button key={opt.id} onClick={() => vote(opt.id)}
            className={`w-full text-left p-2 rounded relative overflow-hidden ${voted ? 'bg-primary-100 border-primary-500' : 'bg-white hover:bg-gray-100'}`}>
            <div className="absolute inset-0 bg-primary-200 opacity-30" style={{ width: `${percent}%` }} />
            <div className="relative flex justify-between">
              <span>{opt.text}</span>
              <span>{percent}% ({opt.voteCount})</span>
            </div>
          </button>
        );
      })}
    </div>
  </div>
)}
```

### Tiêu chí đạt
- ☐ Organizer tạo poll với 2-5 options
- ☐ Volunteer vote 1 lần (nếu single choice)
- ☐ Kết quả hiển thị progress bar + %
- ☐ Hiển thị option đã vote highlight

---

## Mức Khó 7: Realtime chat (SignalR)

### Mục tiêu
Tin nhắn xuất hiện ngay không cần refresh trang. Quan trọng cho ngày diễn ra sự kiện.

### Tool cần
- **NuGet**: `Microsoft.AspNetCore.SignalR.Client` (đã có sẵn trong .NET 8 SDK, không cần cài).
- **npm**: `@microsoft/signalr` — cần cài.

```powershell
cd D:\FW\FW\BaseCore\BaseCore.WebClient
npm install @microsoft/signalr
```

### Bước 1: Tạo Hub

File: `BaseCore.EventService/Hubs/ChannelHub.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BaseCore.Services.VolunteerHub;

[Authorize]
public class ChannelHub : Hub
{
    private readonly IChannelService _channelService;
    
    public ChannelHub(IChannelService channelService) { _channelService = channelService; }
    
    public async Task JoinChannel(int channelId)
    {
        var userId = int.Parse(Context.User!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (!await _channelService.CanAccessChannelAsync(channelId, userId))
            throw new HubException("Not authorized");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel-{channelId}");
    }
    
    public async Task LeaveChannel(int channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel-{channelId}");
    }
}
```

### Bước 2: Đăng ký SignalR

`BaseCore.EventService/Program.cs`:

```csharp
builder.Services.AddSignalR();
// ...
app.MapHub<ChannelHub>("/hubs/channel");
```

Cấu hình JWT cho SignalR:

```csharp
builder.Services.AddAuthentication(...)
    .AddJwtBearer(x => {
        // ... existing config
        x.Events = new JwtBearerEvents {
            OnMessageReceived = context => {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
```

### Bước 3: Broadcast khi có post/comment mới

Inject `IHubContext<ChannelHub>` vào `ChannelService`. Sau khi save post/comment:

```csharp
await _hubContext.Clients.Group($"channel-{channelId}").SendAsync("PostCreated", post);
await _hubContext.Clients.Group($"channel-{channelId}").SendAsync("CommentAdded", new { postId, comment });
```

### Bước 4: Gateway proxy WebSocket

Ocelot không hỗ trợ WebSocket tốt. Có 2 cách:
1. **Dễ**: Frontend kết nối thẳng `http://localhost:5003/hubs/channel` (skip gateway).
2. **Đúng chuẩn**: Dùng YARP thay Ocelot, hoặc cấu hình WebSocket passthrough.

Cho đồ án, chọn cách 1: kết nối thẳng EventService.

### Bước 5: Frontend kết nối

```jsx
import * as signalR from '@microsoft/signalr';

useEffect(() => {
  const token = localStorage.getItem('accessToken');
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`http://localhost:5003/hubs/channel?access_token=${token}`)
    .withAutomaticReconnect()
    .build();
  
  connection.on('PostCreated', (post) => {
    setPosts(prev => [post, ...prev]);
  });
  
  connection.on('CommentAdded', ({ postId, comment }) => {
    // Update comments của post tương ứng
  });
  
  connection.start()
    .then(() => connection.invoke('JoinChannel', parseInt(id)))
    .catch(console.error);
  
  return () => { connection.invoke('LeaveChannel', parseInt(id)).then(() => connection.stop()); };
}, [id]);
```

### Bước 6: CORS cho SignalR

`Program.cs` của EventService:

```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Bắt buộc cho SignalR
    });
});
```

### Tiêu chí đạt
- ☐ Mở 2 tab cùng channel, post ở tab 1 → tab 2 thấy ngay
- ☐ Comment cũng realtime
- ☐ Khi mất kết nối, tự reconnect
- ☐ User không có quyền không nhận được message

---

## Mức Khó 8: Sub-channel cho từng ca

### Mục tiêu
Mỗi `WorkShift` có thể có channel riêng để volunteer trong ca đó trao đổi. Kế thừa channel chính của event.

### Bước 1: Sửa entity

`Channel.cs`:
```csharp
public int? ParentChannelId { get; set; }
public int? ShiftId { get; set; }
public Channel ParentChannel { get; set; }
public WorkShift Shift { get; set; }
```

### Bước 2: Migration

```powershell
dotnet ef migrations add AddSubChannels --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### Bước 3: Logic tạo sub-channel

Khi tạo shift, optional tạo channel kèm:

```csharp
public async Task<Channel> CreateShiftChannelAsync(int shiftId, int organizerId)
{
    var shift = await _context.WorkShifts.Include(s => s.Event).FirstAsync(s => s.Id == shiftId);
    if (shift.Event.OrganizerId != organizerId) throw new Exception("Not authorized");
    
    var parentChannel = await _context.Channels.FirstAsync(c => c.EventId == shift.EventId && c.ParentChannelId == null);
    
    var subChannel = new Channel {
        EventId = shift.EventId,
        Name = $"{parentChannel.Name} - {shift.Name}",
        ParentChannelId = parentChannel.Id,
        ShiftId = shiftId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    _context.Channels.Add(subChannel);
    await _context.SaveChangesAsync();
    return subChannel;
}
```

### Bước 4: Sửa `CanAccessChannelAsync`

Volunteer chỉ vào sub-channel của ca mình đăng ký:

```csharp
if (channel.ShiftId.HasValue) {
    var inShift = await _context.Registrations.AnyAsync(r => 
        r.EventId == channel.EventId && r.UserId == userId && 
        r.ShiftId == channel.ShiftId.Value && r.Status == "Confirmed");
    return inShift;
}
// Existing logic for main channel
```

### Bước 5: Frontend

`Channel.jsx` thêm sidebar list sub-channels:

```jsx
{channel?.subChannels?.length > 0 && (
  <div className="card p-3">
    <p className="text-xs font-semibold text-gray-500 mb-2">CÁC CA</p>
    <div className="space-y-1">
      <Link to={`/channels/${parentId}`} className="block p-2 rounded hover:bg-gray-100">
        <i className="fa-solid fa-hashtag" /> Chính
      </Link>
      {channel.subChannels.map(sc => (
        <Link key={sc.id} to={`/channels/${sc.id}`} className="block p-2 rounded hover:bg-gray-100">
          <i className="fa-solid fa-clock" /> {sc.shiftName}
        </Link>
      ))}
    </div>
  </div>
)}
```

### Tiêu chí đạt
- ☐ Organizer bật/tắt sub-channel cho từng ca
- ☐ Volunteer chỉ thấy sub-channel của ca mình
- ☐ Chuyển qua lại giữa channel chính và sub-channel
- ☐ Post ở sub-channel không xuất hiện ở channel chính

---

## Lộ trình triển khai gợi ý

| Tuần | Tính năng | Người làm |
|---|---|---|
| Tuần 5 (test/fix bug) | Dễ 1 + Dễ 2 + Dễ 3 | B (Event team) |
| Tuần 5 | TB 4 (file attachment) | B |
| Tuần 5-6 | TB 5 (mention) hoặc TB 6 (poll) — chọn 1 | B |
| Sau đồ án | Khó 7 + Khó 8 | Optional, tùy thời gian |

**Khuyến nghị cho đồ án**: làm **Dễ 1 + Dễ 2 + Dễ 3 + TB 6 (Poll)**. Đủ ấn tượng demo, không tốn quá nhiều thời gian, không cần tool mới.

---

## Tool đăng ký/cài đặt

Hiện tại với toàn bộ kế hoạch trên:

| Tool | Cần khi nào | Cài đặt |
|---|---|---|
| .NET 8 SDK | Đã có | — |
| Node.js + npm | Đã có | — |
| Visual Studio / VS Code | Đã có | — |
| `@microsoft/signalr` (npm) | CHỈ khi làm Khó 7 | `npm install @microsoft/signalr` |
| (Không cần đăng ký dịch vụ ngoài nào) | | |

Tất cả tính năng đều dùng tool có sẵn, không cần đăng ký Firebase/Pusher/etc.
