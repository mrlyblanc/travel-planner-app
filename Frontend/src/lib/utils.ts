import clsx from 'clsx';

export const cn = (...values: Array<string | false | null | undefined>) => clsx(values);

export const uid = (prefix: string) =>
  `${prefix}-${Math.random().toString(36).slice(2, 10)}-${Date.now().toString(36)}`;

export const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 0,
});

export const clampNumber = (value: number, min = 0) => Math.max(min, value);
