// A label + input (or textarea) + red error message, used by every form
export default function FormField({
  label,
  name,
  value,
  onChange,
  error,
  type = 'text',
  as = 'input',
  required = false,
  ...rest
}) {
  const Tag = as;
  const id = `field-${name}`;

  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label">
        {label}
        {required && <span className="text-danger"> *</span>}
      </label>
      <Tag
        id={id}
        name={name}
        type={as === 'input' ? type : undefined}
        className={`form-control${error ? ' is-invalid' : ''}`}
        value={value}
        onChange={onChange}
        {...rest}
      />
      {error && <div className="invalid-feedback">{error}</div>}
    </div>
  );
}
