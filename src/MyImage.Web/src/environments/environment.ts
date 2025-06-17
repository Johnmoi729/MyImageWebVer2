export const environment = {
  production: false,
  apiUrl: 'https://localhost:7037/api',
  maxFileSize: 52428800, // 50MB
  supportedFormats: ['.jpg', '.jpeg'],
  encryptionPublicKey: '', // RSA public key for credit cards
  tokenKey: 'myimage_token'
};
