import { NavLink } from 'react-router-dom';
import { useAuth } from '../../context/useAuth';
import { ACCESS } from '../../utils/roles';

// roles: null means every logged-in user
const MENU = [
  { to: '/dashboard', label: 'Dashboard', roles: null },
  { to: '/requests', label: 'Purchase Requests', roles: null },
  { to: '/approvals', label: 'Approvals', roles: ACCESS.approvals },
  { to: '/orders', label: 'Purchase Orders', roles: ACCESS.orders },
  { to: '/vendors', label: 'Vendors', roles: null },
  { to: '/products', label: 'Products', roles: null },
  { to: '/users', label: 'Users', roles: ACCESS.users },
  { to: '/audit-logs', label: 'Audit Log', roles: ACCESS.auditLogs },
];

export default function Sidebar({ open, onNavigate }) {
  const { user } = useAuth();
  const items = MENU.filter((item) => !item.roles || item.roles.includes(user.role));

  return (
    <nav className={`app-sidebar${open ? ' open' : ''}`}>
      <ul className="nav flex-column">
        {items.map((item) => (
          <li className="nav-item" key={item.to}>
            <NavLink to={item.to} className="nav-link" onClick={onNavigate}>
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
