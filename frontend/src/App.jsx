import { Navigate, Route, Routes } from 'react-router-dom';
import ProtectedRoute from './routes/ProtectedRoute';
import Layout from './components/layout/Layout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import VendorsPage from './pages/vendors/VendorsPage';
import ProductsPage from './pages/products/ProductsPage';
import RequestsPage from './pages/requests/RequestsPage';
import RequestDetailPage from './pages/requests/RequestDetailPage';
import RequestFormPage from './pages/requests/RequestFormPage';
import ApprovalsPage from './pages/approvals/ApprovalsPage';
import OrdersPage from './pages/orders/OrdersPage';
import OrderDetailPage from './pages/orders/OrderDetailPage';
import ComingSoonPage from './pages/ComingSoonPage';
import UnauthorizedPage from './pages/UnauthorizedPage';
import NotFoundPage from './pages/NotFoundPage';
import { ACCESS } from './utils/roles';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Everything below needs a logged-in user */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/requests" element={<RequestsPage />} />
          <Route path="/requests/:id" element={<RequestDetailPage />} />
          <Route element={<ProtectedRoute roles={ACCESS.requestCreate} />}>
            <Route path="/requests/new" element={<RequestFormPage />} />
            <Route path="/requests/:id/edit" element={<RequestFormPage />} />
          </Route>
          <Route path="/vendors" element={<VendorsPage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />

          {/* Role-restricted screens */}
          <Route element={<ProtectedRoute roles={ACCESS.approvals} />}>
            <Route path="/approvals" element={<ApprovalsPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={ACCESS.orders} />}>
            <Route path="/orders" element={<OrdersPage />} />
            <Route path="/orders/:id" element={<OrderDetailPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={ACCESS.users} />}>
            <Route path="/users" element={<ComingSoonPage title="Users" />} />
          </Route>
          <Route element={<ProtectedRoute roles={ACCESS.auditLogs} />}>
            <Route path="/audit-logs" element={<ComingSoonPage title="Audit Log" />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}





