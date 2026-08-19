using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Models;
using SmartWMS.Api.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// DB 연결 문자열 조회
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection 연결 문자열을 찾을 수 없습니다.");

// Controller 등록
builder.Services.AddControllers();

// DTO Validation 실패 시 공통 오류 응답 형식 사용
builder.Services.Configure<ApiBehaviorOptions>(options => {
    options.InvalidModelStateResponseFactory = context => {
        // Validation 오류 메시지를 하나로 합침
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var response = new ApiErrorResponse {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = "요청 값이 올바르지 않습니다.",
            Detail = errors.Count > 0
                ? string.Join(" | ", errors)
                : null
        };

        return new BadRequestObjectResult(response);
    };
});

// EF Core DbContext 등록
builder.Services.AddDbContext<SmartWmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Swagger 등록
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => {
    // Swagger에서 JWT Bearer 토큰을 입력할 수 있도록 설정
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "JWT Access Token을 입력하세요."
    });

    // Swagger 요청에 Bearer 인증 적용
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 전역 예외 처리 등록
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Hash 등록
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// JWT 토큰 서비스 등록
builder.Services.AddScoped<JwtTokenService>();

// JWT 설정 조회
var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer 설정이 없습니다.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience 설정이 없습니다.");

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key 설정이 없습니다.");

// JWT 인증 등록
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,

            // 토큰 만료시간을 정확하게 검증
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 개발 환경에서만 Swagger 사용
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Controller 요청 처리 전에 전역 예외 처리 적용
app.UseExceptionHandler();

app.UseHttpsRedirection();

// JWT 인증
app.UseAuthentication();

// 인증된 사용자의 권한 확인
app.UseAuthorization();

app.MapControllers();

app.Run();