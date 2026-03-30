import clsx from 'clsx';

export const cn = (...values: Array<string | false | null | undefined>) => clsx(values);

export const uid = (prefix: string) =>
  `${prefix}-${Math.random().toString(36).slice(2, 10)}-${Date.now().toString(36)}`;

export const clampNumber = (value: number, min = 0) => Math.max(min, value);

export const roundCurrency = (value: number, fractionDigits = 2) => {
  const scale = 10 ** fractionDigits;
  return Math.round((value + Number.EPSILON) * scale) / scale;
};

export const initialsFromName = (value: string) =>
  value
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
