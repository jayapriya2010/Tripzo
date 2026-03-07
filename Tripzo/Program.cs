using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.Repositories;
using AutoMapper;
using Tripzo.Mapping;


namespace Tripzo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("constr")));
            

            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<IFleetRepository, FleetRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();

            // AutoMapper registration
            builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

            // Register Global Exception Handler
            builder.Services.AddExceptionHandler<Tripzo.Middleware.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            
  

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global exception handler should be one of the first middlewares
            app.UseExceptionHandler();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
