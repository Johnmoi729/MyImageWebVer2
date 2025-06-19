export const environment = {
  production: false,
  // CHANGED: Direct communication with backend instead of using proxy
  // This points directly to your ASP.NET Core API running on HTTPS port
  apiUrl: 'https://localhost:7037/api',

  // Photo upload settings
  maxFileSize: 52428800, // 50MB
  supportedFormats: ['.jpg', '.jpeg'],

  // UI configuration
  defaultPageSize: 20,
  imageConfig: {
    thumbnailMaxWidth: 300,
    thumbnailMaxHeight: 300,
    galleryPageSize: 12
  },

  // Debug settings for development
  enableDebugMode: true,
  logApiCalls: true,

  // Encrytion key
  encryptionPublicKey: '', // RSA public key for credit cards
  tokenKey: 'myimage_token'
};
