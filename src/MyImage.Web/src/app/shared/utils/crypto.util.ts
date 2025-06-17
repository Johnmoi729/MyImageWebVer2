export class CryptoUtil {
  // Simple Base64 encoding for demo purposes
  // In production, use proper RSA encryption with the backend's public key
  static encryptCreditCard(cardData: string): string {
    // This is a placeholder implementation
    // In a real application, you would use the RSA public key from the backend
    return btoa(cardData); // Base64 encoding as placeholder
  }

  // Format credit card number for display
  static formatCardNumber(cardNumber: string): string {
    const cleaned = cardNumber.replace(/\s/g, '');
    const match = cleaned.match(/^(\d{4})(\d{4})(\d{4})(\d{4})$/);

    if (match) {
      return `${match[1]} ${match[2]} ${match[3]} ${match[4]}`;
    }

    return cardNumber;
  }

  // Get card type from number
  static getCardType(cardNumber: string): string {
    const cleaned = cardNumber.replace(/\s/g, '');

    if (/^4/.test(cleaned)) {
      return 'Visa';
    } else if (/^5[1-5]/.test(cleaned)) {
      return 'MasterCard';
    } else if (/^3[47]/.test(cleaned)) {
      return 'American Express';
    } else if (/^6(?:011|5)/.test(cleaned)) {
      return 'Discover';
    }

    return 'Unknown';
  }

  // Mask credit card number for display
  static maskCardNumber(cardNumber: string): string {
    const cleaned = cardNumber.replace(/\s/g, '');
    if (cleaned.length < 4) return cardNumber;

    const lastFour = cleaned.slice(-4);
    const masked = '**** **** **** ' + lastFour;
    return masked;
  }
}
