using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Transactions.Data;
using Transactions.Data.Repository;
using Transactions.Domain.Configuration;
using Transactions.Domain.Services;

using Transactions.Domain.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.Configure<TransactionSettings>(
    builder.Configuration.GetSection(TransactionSettings.SectionName));

builder.Services.AddValidatorsFromAssemblyContaining<TransactionPayloadValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Required to make Program class accessible to the Test project
public partial class Program { }