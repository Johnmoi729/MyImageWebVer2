# MyImage Photo Printing Service

A modern photo printing web application built with ASP.NET Core Web API and Angular 19 following clean architecture principles.

## Technology Stack

### Backend (Week 1 Development Focus)
- **ASP.NET Core Web API 9.0** - RESTful API with controllers
- **MongoDB with GridFS** - Document database with binary file storage
- **Stripe.net 47.2.0** - Payment processing with webhook integration  
- **ImageSharp 3.1.6** - Server-side image processing and thumbnail generation
- **JWT Authentication** - Secure token-based user authentication

### Frontend (Week 2 Development Focus)  
- **Angular 19 with TypeScript 5.6** - Modern single-page application framework
- **Angular Material** - Material Design UI component library
- **RxJS** - Reactive programming for handling async operations
- **Stripe.js** - Client-side payment form integration

## Quick Start Guide

### Prerequisites
- **.NET 9.0 SDK** - Download from https://dotnet.microsoft.com/download/dotnet/9.0
- **Node.js 18+** - Download from https://nodejs.org/
- **Docker** - For running MongoDB development database

### Development Setup Instructions

1. **Start the development database:**
   `ash
   docker-compose up -d
   `
   This starts MongoDB on port 27017 and Mongo Express web UI on port 8081

2. **Configure Stripe payment keys (Required for payment functionality):**
   - Create a Stripe account and get test keys from https://dashboard.stripe.com/test/apikeys
   - Update the StripeSettings section in src/MyImage.API/appsettings.json
   - Update stripePublishableKey in src/MyImage.Web/src/environments/environment.ts

3. **Run the backend API server:**
   `ash
   cd src/MyImage.API
   dotnet run
   `
   - API will be available at: https://localhost:5001
   - Swagger documentation: https://localhost:5001/swagger

4. **Run the frontend application (in a new terminal):**
   `ash
   cd src/MyImage.Web  
   ng serve
   `
   - Web application will be available at: http://localhost:4200

## Project Architecture Overview

This project follows clean architecture principles with clear separation of concerns and proper dependency management:

`
src/
笏懌楳笏 MyImage.API/           # Presentation Layer (Week 1 Backend Development)
笏・  笏懌楳笏 Controllers/       # HTTP request handling and API endpoints
笏・  笏懌楳笏 Middleware/        # Cross-cutting concerns (auth, error handling)
笏・  笏披楳笏 Program.cs         # Application startup and dependency injection
笏懌楳笏 MyImage.Core/          # Domain Layer (Week 1 Backend Development)  
笏・  笏懌楳笏 Entities/          # Business entities (User, Photo, Order, ShoppingCart)
笏・  笏懌楳笏 Interfaces/        # Repository and service contracts
笏・  笏披楳笏 DTOs/              # Data transfer objects for API communication
笏懌楳笏 MyImage.Infrastructure/ # Infrastructure Layer (Week 1 Backend Development)
笏・  笏懌楳笏 Repositories/      # MongoDB data access implementations
笏・  笏懌楳笏 Services/          # External service integrations (Stripe, ImageSharp)
笏・  笏披楳笏 Data/              # Database context and GridFS configuration
笏披楳笏 MyImage.Web/           # Client Application (Week 2 Frontend Development)
    笏懌楳笏 src/app/core/      # Singleton services and route guards  
    笏懌楳笏 src/app/shared/    # Reusable UI components
    笏披楳笏 src/app/features/  # Feature modules (auth, photos, shopping, admin)
`

### Architectural Benefits for 3-Week Development
- **Clear Dependencies**: Core has no dependencies, Infrastructure depends only on Core, API orchestrates everything
- **Parallel Development**: Backend developer can work independently in Week 1, frontend developer in Week 2
- **Testability**: Each layer can be unit tested independently with mocked dependencies  
- **Integration Ready**: Week 3 becomes true integration rather than debugging architectural issues

## Core Application Features

### User Management System
- User registration with auto-generated readable User IDs (USR-YYYY-NNNNNN format)
- JWT-based secure authentication for all API endpoints
- Role-based authorization distinguishing Customers from Administrators

### Photo Management Workflow  
- Desktop folder scanning to display only JPEG files from selected directories
- Bulk photo upload with real-time progress tracking and error handling
- GridFS storage system for efficient large file handling and retrieval
- Automatic thumbnail generation for fast gallery loading
- Complete photo lifecycle tracking (uploaded 竊・ordered 竊・shipped 竊・automatically deleted)

### Shopping and Ordering Experience
- Multiple print sizes and quantities can be selected per individual photo
- Real-time price calculations in shopping cart with tax computation
- Tax calculation automatically based on customer shipping address
- Complete order history with detailed status tracking for customers

### Payment Processing Integration
- Stripe integration for secure credit card processing with client-side encryption
- Alternative branch payment option for customers who prefer in-person payment
- All payment data is encrypted before transmission to ensure PCI compliance
- Manual admin payment verification workflow for quality control

### Administrative Management Features
- Comprehensive order management with status updates and filtering
- Print size and pricing administration with immediate effect on new orders
- Order completion workflow that triggers automatic photo cleanup
- Admin dashboard displaying key metrics and orders requiring attention

## Development Workflow (3-Week Timeline)

### Week 1: Complete Backend Development (Backend Developer Focus)
- **Focus**: Complete API development, database setup, and business logic implementation
- **Developer**: Backend specialist working independently
- **Deliverable**: Fully functional REST API tested thoroughly with Postman/Swagger
- **Key Components**: All entities, repositories, services, controllers, and authentication

### Week 2: Complete Frontend Development (Frontend Developer Focus)  
- **Focus**: Complete Angular application development and user interface creation
- **Developer**: Frontend specialist working independently or with completed backend
- **Deliverable**: Complete responsive UI tested with mock data or integrated with backend API
- **Key Components**: All components, services, routing, and user workflows

### Week 3: Integration, Testing, and Deployment (Team Collaboration)
- **Focus**: Connect frontend to backend, comprehensive testing, bug fixes
- **Team**: Both developers collaborating closely on integration issues
- **Deliverable**: Production-ready application with full end-to-end functionality
- **Key Activities**: API integration, user acceptance testing, deployment setup

## Development Environment URLs

- **Frontend Web Application**: http://localhost:4200
- **Backend REST API**: https://localhost:5001  
- **API Documentation (Swagger)**: https://localhost:5001/swagger
- **MongoDB Database Interface**: http://localhost:8081
- **MongoDB Direct Connection**: mongodb://localhost:27017

## Security Implementation Details

- **Password Security**: BCrypt hashing with salt for all user passwords
- **Payment Security**: Client-side RSA encryption before transmission, no plain-text card data storage
- **File Security**: JPEG-only validation, 50MB size limits, secure file naming conventions
- **API Security**: JWT tokens with proper expiration, CORS properly configured for frontend origin
- **Database Security**: Input validation and parameterized queries through MongoDB driver

## Business Rules and Data Management

- **Photo Access Control**: Users can only access and manage their own uploaded photos
- **Order Data Integrity**: Photos cannot be deleted once they become part of any order  
- **Payment Verification**: Manual admin verification step required for all payment methods
- **Storage Management**: Automatic photo deletion after order completion and shipping confirmation
- **File Restrictions**: Only JPEG format accepted with 50MB maximum file size per photo

## Next Steps After Setup

1. **Configure Stripe**: Update both backend and frontend with your Stripe test keys
2. **Start Development**: Begin with backend entities and repositories in Week 1
3. **Test Thoroughly**: Use Swagger and Postman to test each API endpoint as you build
4. **Document Progress**: Keep track of completed features for smooth Week 3 integration

This project structure supports rapid development while maintaining high code quality and preparing for future enhancements and scaling.
