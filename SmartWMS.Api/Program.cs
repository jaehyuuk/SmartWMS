using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection 연결 문자열을 찾을 수 없습니다.");

// Controller 등록
builder.Services.AddControllers();

// DTO Validation 실패 시 공통 오류 응답 형식 사용
builder.Services.Configure<ApiBehaviorOptions>(options => {
    options.InvalidModelStateResponseFactory = context => {
        // Validation 오류 메시지를 하나의 문자열로 합침
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

// DbContext 등록
builder.Services.AddDbContext<SmartWmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 전역 예외 처리
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Controller 실행보다 먼저 등록되어 있어야 함
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();