import { orderStatusInfo } from '../../utils/orderStatus';

export default function OrderStatusBadge({ status, overdue = false }) {
  const info = orderStatusInfo(status);
  return (
    <>
      <span className={`badge ${info.badge}`}>{info.label}</span>
      {overdue && <span className="badge text-bg-danger ms-1">Overdue</span>}
    </>
  );
}
