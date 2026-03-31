import { z } from 'zod';

export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_MAX_LENGTH = 128;

export const passwordPolicyHelperText =
  'Use 8-128 characters with at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.';

const hasUppercase = (value: string) => /\p{Lu}/u.test(value);
const hasLowercase = (value: string) => /\p{Ll}/u.test(value);
const hasDigit = (value: string) => /\p{Nd}/u.test(value);
const hasSpecialCharacter = (value: string) => /[^\p{L}\p{N}]/u.test(value);

export const createExistingPasswordSchema = (fieldLabel: string) =>
  z
    .string()
    .min(PASSWORD_MIN_LENGTH, `${fieldLabel} must be at least ${PASSWORD_MIN_LENGTH} characters`)
    .max(PASSWORD_MAX_LENGTH, `${fieldLabel} must be ${PASSWORD_MAX_LENGTH} characters or fewer`);

export const createStrongPasswordSchema = (fieldLabel: string) =>
  createExistingPasswordSchema(fieldLabel)
    .refine((value) => hasUppercase(value), `${fieldLabel} must include an uppercase letter`)
    .refine((value) => hasLowercase(value), `${fieldLabel} must include a lowercase letter`)
    .refine((value) => hasDigit(value), `${fieldLabel} must include a number`)
    .refine((value) => hasSpecialCharacter(value), `${fieldLabel} must include a special character`);
