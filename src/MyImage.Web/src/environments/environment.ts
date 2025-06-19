export const environment = {
  production: false,
  // CHANGED: Direct communication with backend instead of using proxy
  // This points directly to your ASP.NET Core API running on HTTPS port
  apiUrl: 'https://localhost:7037/api',
  maxFileSize: 52428800, // 50MB
  supportedFormats: ['.jpg', '.jpeg'],
  encryptionPublicKey: '', // RSA public key for credit cards
  tokenKey: 'myimage_token'
};
