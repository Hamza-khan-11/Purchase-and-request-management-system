import { statusInfo } from '../../utils/requestStatus';

export default function RequestStatusBadge({ status }) {
  const info = statusInfo(status);
  return <span className={`badge ${info.badge}`}>{info.label}</span>;
}
