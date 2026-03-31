import currencyCatalog from '../../../shared/currency-catalog.json';
import { roundCurrency } from './utils';
import type { ItineraryEvent } from '../types/event';

export interface CurrencyOption {
  code: string;
  name: string;
  minorUnit: number;
}

export interface CurrencyTotal {
  currencyCode: string;
  amount: number;
}

export interface CurrencySummaryDisplay {
  primaryLabel: string;
  secondaryLabels: string[];
  remainingCount: number;
  totalCount: number;
}

const formatterCache = new Map<string, Intl.NumberFormat>();

export const supportedCurrencies = currencyCatalog as CurrencyOption[];

const currencyLookup = supportedCurrencies.reduce<Record<string, CurrencyOption>>((accumulator, currency) => {
  accumulator[currency.code] = currency;
  return accumulator;
}, {});

export const normalizeCurrencyCode = (currencyCode: string | null | undefined) => {
  const normalized = currencyCode?.trim().toUpperCase() ?? '';
  return normalized || null;
};

export const getCurrencyOption = (currencyCode: string | null | undefined) => {
  const normalized = normalizeCurrencyCode(currencyCode);
  return normalized ? currencyLookup[normalized] ?? null : null;
};

export const getCurrencyMinorUnit = (currencyCode: string | null | undefined) =>
  getCurrencyOption(currencyCode)?.minorUnit ?? 2;

export const getCurrencyStep = (currencyCode: string | null | undefined) => {
  const minorUnit = getCurrencyMinorUnit(currencyCode);
  if (minorUnit <= 0) {
    return '1';
  }

  return `0.${'0'.repeat(Math.max(0, minorUnit - 1))}1`;
};

export const roundCurrencyAmount = (amount: number, currencyCode: string | null | undefined) =>
  roundCurrency(amount, getCurrencyMinorUnit(currencyCode));

export const hasValidCurrencyPrecision = (amount: number, currencyCode: string | null | undefined) =>
  roundCurrencyAmount(amount, currencyCode) === amount;

export const formatEditableCurrencyAmount = (amount: number, currencyCode: string | null | undefined) =>
  roundCurrencyAmount(amount, currencyCode).toFixed(getCurrencyMinorUnit(currencyCode));

export const getCurrencyOptionLabel = (currencyCode: string) => {
  const normalized = normalizeCurrencyCode(currencyCode);
  if (!normalized) {
    return 'No currency selected';
  }

  const option = currencyLookup[normalized];
  return option ? `${option.code} • ${option.name}` : normalized;
};

export const formatCurrencyAmount = (amount: number, currencyCode: string | null | undefined) => {
  const normalizedCurrencyCode = normalizeCurrencyCode(currencyCode);
  const minorUnit = getCurrencyMinorUnit(normalizedCurrencyCode);
  const roundedAmount = roundCurrencyAmount(amount, normalizedCurrencyCode);

  if (!normalizedCurrencyCode) {
    return roundedAmount.toFixed(minorUnit);
  }

  const formatterKey = `${normalizedCurrencyCode}:${minorUnit}`;
  const cachedFormatter = formatterCache.get(formatterKey);
  if (cachedFormatter) {
    return cachedFormatter.format(roundedAmount);
  }

  try {
    const formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: normalizedCurrencyCode,
      minimumFractionDigits: minorUnit,
      maximumFractionDigits: minorUnit,
    });

    formatterCache.set(formatterKey, formatter);
    return formatter.format(roundedAmount);
  } catch {
    return `${normalizedCurrencyCode} ${roundedAmount.toFixed(minorUnit)}`;
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
        roundCurrencyAmount((accumulator.get(normalizedCurrencyCode) ?? 0) + event.cost, normalizedCurrencyCode),
      );
      return accumulator;
    }, new Map<string, number>()),
  )
    .map(([currencyCode, amount]) => ({ currencyCode, amount }))
    .sort((left, right) => right.amount - left.amount);

export const getCurrencySummaryDisplay = (
  totals: CurrencyTotal[],
  {
    maxSecondary = 2,
  }: {
    maxSecondary?: number;
  } = {},
): CurrencySummaryDisplay | null => {
  if (totals.length === 0) {
    return null;
  }

  const [primary, ...secondaryTotals] = totals;
  const secondaryLabels = secondaryTotals
    .slice(0, Math.max(0, maxSecondary))
    .map((total) => formatCurrencyAmount(total.amount, total.currencyCode));

  return {
    primaryLabel: formatCurrencyAmount(primary.amount, primary.currencyCode),
    secondaryLabels,
    remainingCount: Math.max(0, secondaryTotals.length - secondaryLabels.length),
    totalCount: totals.length,
  };
};

export const formatCompactCurrencySummary = (
  totals: CurrencyTotal[],
  {
    emptyLabel = 'No costs yet',
  }: {
    emptyLabel?: string;
  } = {},
) => {
  const summary = getCurrencySummaryDisplay(totals, { maxSecondary: 0 });
  if (!summary) {
    return emptyLabel;
  }

  return summary.totalCount > 1 ? `${summary.primaryLabel} +${summary.totalCount - 1}` : summary.primaryLabel;
};

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
