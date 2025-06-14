# 🎓 Learning Management System (LMS)

A comprehensive Learning Management System built with **ASP.NET Core 8** following **Clean Architecture** principles and modern design patterns.

## ✨ Features

### 👥 User Management
- **Multi-role Authentication** (Student, Instructor, Admin)
- **JWT Token-based Security**
- **BCrypt Password Hashing**
- User registration and login

### 📚 Course Management
- Create and manage courses with categories
- Multiple course levels (Beginner, Intermediate, Advanced)
- Course publishing workflow
- Instructor-specific course management

### 📖 Learning Experience
- **Structured Lessons** with video content
- **Progress Tracking** per lesson and course
- **Assignment System** with submissions and grading
- **Course Reviews and Ratings**
- Enrollment management

### 🎯 Assessment System
- Multiple assignment types (Quiz, Essay, Project, Exam)
- Automated progress calculation
- Instructor feedback and grading
- Course completion certificates

## 🏗️ Architecture

This project follows **Clean Architecture** with clear separation of concerns:

```
📁 LMS.API          # Web API Layer (RESTful API, JWT Auth)
📁 LMS.Web          # MVC Web Application (User Interface)
📁 LMS.Application  # Application Layer (Services, DTOs)
📁 LMS.Core         # Domain Layer (Models, Interfaces, Enums)
📁 LMS.Infrastructure # Infrastructure Layer (Data Access, Repositories)
📁 LMS.Shared       # Shared Utilities (Extensions, Helpers)
```

### 🎯 Design Patterns Used
- **Repository Pattern** for data access abstraction
- **Dependency Injection** for loose coupling
- **Service Layer Pattern** for business logic
- **DTO Pattern** for data transfer
- **Generic Repository** for code reusability

## 🛠️ Tech Stack

### Backend
- **ASP.NET Core 8** - Web API Framework
- **ASP.NET Core MVC 8** - Web Application Frontend
- **Entity Framework Core 8** - ORM
- **SQL Server** - Database
- **JWT Authentication** - API Security
- **Cookie Authentication** - MVC Session Management
- **BCrypt.NET** - Password Hashing

### Frontend (MVC)
- **Razor Views** - Server-side rendering
- **Bootstrap 5** - CSS Framework
- **jQuery** - JavaScript library
- **DataTables** - Advanced table functionality
- **Chart.js** - Data visualization
- **Select2** - Enhanced dropdowns

### Tools & Libraries
- **Swagger/OpenAPI** - API Documentation
- **AutoMapper** (Ready for implementation)
- **FluentValidation** (Ready for implementation)

## 📊 Database Schema

### Core Entities
- **👤 Users** - Multi-role user management
- **📚 Courses** - Course information and metadata
- **📖 Lessons** - Individual learning units
- **📝 Assignments** - Assessment and evaluation
- **📊 Enrollments** - Student-course relationships
- **📈 LessonProgress** - Detailed progress tracking

### Key Relationships
```
User (1) ────── (N) Course [Instructor]
User (N) ────── (N) Course [via Enrollments]
Course (1) ──── (N) Lesson
Course (1) ──── (N) Assignment
Enrollment (1) ─ (N) LessonProgress
```

## 🚀 Getting Started

### Prerequisites
- **.NET 8 SDK**
- **SQL Server** (LocalDB works fine)
- **Visual Studio 2022** or **VS Code**

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/LMS.git
   cd LMS
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Update connection string**
   ```json
   // appsettings.json (both API and Web projects)
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LMSDB;Trusted_Connection=true"
     }
   }
   ```

4. **Run migrations**
   ```bash
   dotnet ef database update --project LMS.Infrastructure --startup-project LMS.API
   ```

5. **Run both applications**
   
   **Option A: Multiple Startup Projects (Recommended)**
   - Right-click solution → Set Startup Projects
   - Select "Multiple startup projects"
   - Set both LMS.API and LMS.Web to "Start"
   
   **Option B: Separate Terminals**
   ```bash
   # Terminal 1 - API
   dotnet run --project LMS.API
   
   # Terminal 2 - Web
   dotnet run --project LMS.Web
   ```

6. **Access the applications**
   ```
   🔗 Web App (MVC):  https://localhost:7095
   🔗 API:           https://localhost:7278/swagger
   ```

## 🌐 Application Access

### 🖥️ Web Application (MVC)
- **URL**: `https://localhost:7095`
- **Features**: Full user interface for all LMS functionality
- **Authentication**: Cookie-based sessions
- **Responsive**: Mobile-friendly Bootstrap design

### 🔧 API Documentation
- **Swagger UI**: `https://localhost:7278/swagger`
- **Features**: Complete REST API documentation
- **Authentication**: JWT token-based
- **Testing**: Interactive API testing interface

## 📝 API Endpoints

### 🔐 Authentication
```http
POST /api/auth/register  # User registration
POST /api/auth/login     # User login
```

### 📚 Courses
```http
GET    /api/courses              # Get all published courses
GET    /api/courses/{id}         # Get course by ID
POST   /api/courses              # Create new course (Instructor+)
POST   /api/courses/{id}/enroll  # Enroll in course
GET    /api/courses/my-courses   # Get user's enrolled courses
```

### 📖 Lessons
```http
GET    /api/lessons/course/{courseId}  # Get course lessons
GET    /api/lessons/{id}               # Get lesson details
POST   /api/lessons/{id}/complete      # Mark lesson complete
POST   /api/lessons                    # Create lesson (Instructor+)
```

### 🏷️ Categories
```http
GET    /api/categories     # Get all categories
GET    /api/categories/{id} # Get category by ID
POST   /api/categories     # Create category
```

## 🎯 Sample Data

The system includes seed data with:
- **Admin User**: `admin@lms.com` / `admin123`
- **Instructor**: `instructor@lms.com` / `instructor123`
- **Sample Course**: "Introduction to Web Development"
- **Categories**: Programming, Design, Business, Marketing, Data Science

### 🔑 Default Login Credentials

| Role | Email | Password | Access |
|------|-------|----------|---------|
| **Admin** | admin@lms.com | admin123 | Full system access |
| **Instructor** | instructor@lms.com | instructor123 | Course creation & management |
| **Student** | Create new account | - | Course enrollment & learning |

## 🔧 Configuration

### JWT Settings
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "LMS-API",
    "Audience": "LMS-Users",
    "ExpiryInDays": 7
  }
}
```

### File Upload Settings
```json
{
  "FileStorage": {
    "UploadPath": "wwwroot/uploads",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".mp4"]
  }
}
```

## 🧪 Testing

### Sample API Calls

**Register a new user:**
```bash
curl -X POST "https://localhost:7278/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Ahmed",
    "lastName": "Ali",
    "email": "ahmed@example.com",
    "password": "password123",
    "role": 1
  }'
```

**Create a course:**
```bash
curl -X POST "https://localhost:7278/api/courses" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Advanced C# Programming",
    "description": "Deep dive into C# concepts",
    "categoryId": 1,
    "level": 2,
    "duration": 60
  }'
```

## 🔮 Future Enhancements

- [ ] **Real-time notifications** with SignalR
- [ ] **Video streaming** integration
- [ ] **Mobile app** with Xamarin/MAUI
- [ ] **Advanced analytics** dashboard
- [ ] **Discussion forums** for courses
- [ ] **Payment integration** for paid courses
- [ ] **Certificates** generation
- [ ] **Multi-language support**
- [ ] **PWA support** for offline learning
- [ ] **Dark mode** theme option
- [ ] **Advanced search** with filters
- [ ] **Bulk operations** for admin panel

## 📱 Architecture Benefits

### 🎯 **Dual Frontend Approach**
- **MVC Web App**: Traditional server-side rendering for SEO and performance
- **REST API**: Enables future mobile apps, SPAs, or third-party integrations

### 🔧 **Shared Business Logic**
- Both applications use the same **Application** and **Core** layers
- Consistent business rules and data validation
- Single source of truth for all operations

### 📈 **Scalability**
- API can handle multiple client types (Web, Mobile, Desktop)
- Database optimized with proper indexing and relationships
- Clean separation allows independent scaling of components

## 📋 Project Structure Details

### 🎯 Core Layer (`LMS.Core`)
Contains the domain entities, enums, and interfaces:
- **Models**: User, Course, Lesson, Assignment, etc.
- **Enums**: UserRole, CourseLevel, CourseStatus
- **Interfaces**: Repository and Service contracts

### 🏢 Application Layer (`LMS.Application`)
Business logic and application services:
- **Services**: Business logic implementation
- **DTOs**: Data transfer objects for API
- **Mapping**: Entity ↔ DTO conversions

### 🏗️ Infrastructure Layer (`LMS.Infrastructure`)
Data access and external concerns:
- **DbContext**: Entity Framework configuration
- **Repositories**: Data access implementation
- **Migrations**: Database schema changes

### 🌐 Web Layer (`LMS.Web`)
MVC web application providing user interface:
- **Controllers**: Handle HTTP requests and user interactions
- **Views**: Razor templates for UI rendering
- **ViewModels**: Data binding between views and controllers
- **wwwroot**: Static files (CSS, JS, images)
- **Areas**: Organized sections (Admin, Student, Instructor)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Your Name**
- GitHub: (https://github.com/MohamedAshraf144)
- LinkedIn: (https://www.linkedin.com/in/mohamed-ashraf-ata/)
- Email: mohamed.dev321@gmail.com

---

⭐ **Star this repository if you found it helpful!**

Built with ❤️ using ASP.NET Core
