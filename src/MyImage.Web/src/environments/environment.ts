export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  stripePublishableKey: 'pk_test_your_publishable_key_here',
  maxFileSize: 52428800, // 50MB maximum file size for photo uploads
  supportedFormats: ['.jpg', '.jpeg'], // Only JPEG files as per requirements
  thumbnailMaxWidth: 300,
  thumbnailMaxHeight: 300
};
