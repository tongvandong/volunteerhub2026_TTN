import React from 'react';
import { BrowserRouter as Router, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import PublicLayout from './components/layouts/PublicLayout';
import SharedLayout from './components/layouts/SharedLayout';
import ErrorBoundary from './components/ErrorBoundary';
import EmptyState from './components/ui/EmptyState';

const FoundationPage = ({ title, description }) => (
  <div className="container-page section-y">
    <EmptyState title={title} description={description} />
  </div>
);

export default function App() {
  return (
    <Router>
      <AuthProvider>
        <ErrorBoundary>
          <Routes>
            <Route
              path="/"
              element={
                <PublicLayout>
                  <FoundationPage
                    title="VolunteerHub"
                    description="Không gian kết nối hoạt động tình nguyện, ban tổ chức và cộng đồng đóng góp."
                  />
                </PublicLayout>
              }
            />
            <Route
              path="/events"
              element={
                <SharedLayout>
                  <FoundationPage
                    title="Danh sách sự kiện"
                    description="Khám phá các hoạt động tình nguyện đang mở và chọn sự kiện phù hợp."
                  />
                </SharedLayout>
              }
            />
            <Route
              path="/events/:id"
              element={
                <SharedLayout>
                  <FoundationPage
                    title="Chi tiết sự kiện"
                    description="Theo dõi thông tin sự kiện, đăng ký tham gia và cập nhật trạng thái hoạt động."
                  />
                </SharedLayout>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </ErrorBoundary>
      </AuthProvider>
    </Router>
  );
}
