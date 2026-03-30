import { roundCurrency } from './utils';
import type { ItineraryEvent } from '../types/event';

export interface CurrencyOption {
  code: string;
  name: string;
}

export interface CurrencyTotal {
  currencyCode: string;
  amount: number;
}

const formatterCache = new Map<string, Intl.NumberFormat>();

export const supportedCurrencies: CurrencyOption[] = [
  { code: 'USD', name: 'US Dollar' },
  { code: 'JPY', name: 'Japanese Yen' },
  { code: 'KRW', name: 'South Korean Won' },
  { code: 'SGD', name: 'Singapore Dollar' },
  { code: 'IDR', name: 'Indonesian Rupiah' },
  { code: 'PHP', name: 'Philippine Peso' },
  { code: 'EUR', name: 'Euro' },
  { code: 'GBP', name: 'British Pound' },
  { code: 'AUD', name: 'Australian Dollar' },
  { code: 'CAD', name: 'Canadian Dollar' },
];

export const normalizeCurrencyCode = (currencyCode: string | null | undefined) => {
  const normalized = currencyCode?.trim().toUpperCase() ?? '';
  return normalized || null;
};

export const getCurrencyOptionLabel = (currencyCode: string) => {
  const normalized = normalizeCurrencyCode(currencyCode);
  if (!normalized) {
    return 'No currency selected';
  }

  const option = supportedCurrencies.find((entry) => entry.code === normalized);
  return option ? `${option.code} • ${option.name}` : normalized;
};

export const formatCurrencyAmount = (amount: number, currencyCode: string | null | undefined) => {
  const normalizedCurrencyCode = normalizeCurrencyCode(currencyCode);
  const roundedAmount = roundCurrency(amount);

  if (!normalizedCurrencyCode) {
    return roundedAmount.toFixed(2);
  }

  const cachedFormatter = formatterCache.get(normalizedCurrencyCode);
  if (cachedFormatter) {
    return cachedFormatter.format(roundedAmount);
  }

  try {
    const formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: normalizedCurrencyCode,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });

    formatterCache.set(normalizedCurrencyCode, formatter);
    return formatter.format(roundedAmount);
  } catch {
    return `${normalizedCurrencyCode} ${roundedAmount.toFixed(2)}`;
  }
};

export const getCostTotalsByCurrency = (events: ItineraryEvent[]) =>
  Array.from(
    events.reduce<Map<string, number>>((accumulator, event) => {
      const normalizedCurrencyCode = normalizeCurrencyCode(event.currencyCode);
      if (!normalizedCurrencyCode || event.cost <= 0) {
        return accumulator;
      }

      accumulator.set(
        normalizedCurrencyCode,
        roundCurrency((accumulator.get(normalizedCurrencyCode) ?? 0) + event.cost),
      );
      return accumulator;
    }, new Map<string, number>()),
  )
    .map(([currencyCode, amount]) => ({ currencyCode, amount }))
    .sort((left, right) => right.amount - left.amount);

export const formatCurrencySummary = (
  totals: CurrencyTotal[],
  {
    emptyLabel = 'No costs yet',
    maxVisible = 2,
  }: {
    emptyLabel?: string;
    maxVisible?: number;
  } = {},
) => {
  if (totals.length === 0) {
    return emptyLabel;
  }

  const visibleTotals = totals.slice(0, maxVisible).map((total) => formatCurrencyAmount(total.amount, total.currencyCode));
  const remainingCount = totals.length - visibleTotals.length;

  return remainingCount > 0 ? `${visibleTotals.join(' • ')} • +${remainingCount} more` : visibleTotals.join(' • ');
};
