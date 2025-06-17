import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class CustomValidators {
  // Password strength validator
  static passwordStrength(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    const hasNumber = /[0-9]/.test(value);
    const hasUpper = /[A-Z]/.test(value);
    const hasLower = /[a-z]/.test(value);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const valid = hasNumber && hasUpper && hasLower && value.length >= 8;

    if (!valid) {
      return { passwordStrength: true };
    }

    return null;
  }

  // Email format validator (more strict than Angular's default)
  static emailFormat(control: AbstractControl): ValidationErrors | null {
    const email = control.value;

    if (!email) {
      return null;
    }

    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

    if (!emailPattern.test(email)) {
      return { emailFormat: true };
    }

    return null;
  }

  // Phone number validator
  static phoneNumber(control: AbstractControl): ValidationErrors | null {
    const phone = control.value;

    if (!phone) {
      return null;
    }

    // Remove all non-digits
    const digits = phone.replace(/\D/g, '');

    // Must be 10 digits (US phone number)
    if (digits.length !== 10) {
      return { phoneNumber: true };
    }

    return null;
  }

  // Credit card number validator (basic Luhn algorithm)
  static creditCard(control: AbstractControl): ValidationErrors | null {
    const cardNumber = control.value;

    if (!cardNumber) {
      return null;
    }

    // Remove spaces and dashes
    const cleanNumber = cardNumber.replace(/[\s-]/g, '');

    // Check if all digits
    if (!/^\d+$/.test(cleanNumber)) {
      return { creditCard: true };
    }

    // Luhn algorithm
    let sum = 0;
    let alternate = false;

    for (let i = cleanNumber.length - 1; i >= 0; i--) {
      let n = parseInt(cleanNumber.charAt(i), 10);

      if (alternate) {
        n *= 2;
        if (n > 9) {
          n = (n % 10) + 1;
        }
      }

      sum += n;
      alternate = !alternate;
    }

    if (sum % 10 !== 0) {
      return { creditCard: true };
    }

    return null;
  }

  // Confirm password validator
  static confirmPassword(passwordField: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.parent?.get(passwordField);
      const confirmPassword = control.value;

      if (!password || !confirmPassword) {
        return null;
      }

      if (password.value !== confirmPassword) {
        return { confirmPassword: true };
      }

      return null;
    };
  }
}
