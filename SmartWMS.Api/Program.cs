using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Models;
using SmartWMS.Api.Services;

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
builder.Services.AddSwaggerGen();

// 전역 예외 처리 등록
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Hash 등록
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// JWT 토큰 서비스 등록
builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();

// 개발 환경에서만 Swagger 사용
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Controller 요청 처리 전에 전역 예외 처리 적용
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();