using DentalPatientApp.Models;
using DentalPatientApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure the LiteDB path
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    {"DatabaseSettings:LiteDbPath", "DentalPatients.db"}
});

// Add LiteDB patient store
builder.Services.AddSingleton<PatientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Patient API endpoints
app.MapGet("/patients", (PatientService patientService) =>
{
    return Results.Ok(patientService.GetAllPatients());
})
.WithName("GetAllPatients")
.WithOpenApi();

app.MapGet("/patients/{id}", (int id, PatientService patientService) =>
{
    var patient = patientService.GetPatient(id);
    if (patient == null)
        return Results.NotFound();
    
    return Results.Ok(patient);
})
.WithName("GetPatientById")
.WithOpenApi();

app.MapPost("/patients", (Patient patient, PatientService patientService) =>
{
    var newPatient = patientService.AddPatient(patient);
    return Results.Created($"/patients/{newPatient.Id}", newPatient);
})
.WithName("AddPatient")
.WithOpenApi();

app.MapPut("/patients/{id}", (int id, Patient patient, PatientService patientService) =>
{
    if (id != patient.Id)
        return Results.BadRequest("ID mismatch");
        
    var updatedPatient = patientService.UpdatePatient(patient);
    if (updatedPatient == null)
        return Results.NotFound();
        
    return Results.Ok(updatedPatient);
})
.WithName("UpdatePatient")
.WithOpenApi();

app.MapDelete("/patients/{id}", (int id, PatientService patientService) =>
{
    var result = patientService.DeletePatient(id);
    if (!result)
        return Results.NotFound();
        
    return Results.NoContent();
})
.WithName("DeletePatient")
.WithOpenApi();

app.Run();