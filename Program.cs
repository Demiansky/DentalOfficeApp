using DentalPatientApp.Models;
using DentalPatientApp.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. //
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure the LiteDB path //
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    {"DatabaseSettings:LiteDbPath", "DentalPatients.db"}
});

// Add LiteDB patient store //
builder.Services.AddSingleton<PatientService>();

var app = builder.Build();

// Configure the HTTP request pipeline. //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

///////////////////////////
// Patient API endpoints //
///////////////////////////

/// Get All Patients ///
app.MapGet("/patients", (PatientService patientService) =>
{
    return Results.Ok(patientService.GetAllPatients());
})
.WithName("GetAllPatients")
.WithOpenApi(operation => {
    operation.Summary = "Get All Patients";
    operation.Description = "Retrieves a complete list of dental patients in the system";
    return operation;
});

/// Get Patient By Id ///
app.MapGet("/patients/{id}", (Guid id, PatientService patientService) =>
{
    var patient = patientService.GetPatient(id);
    if (patient == null)
        return Results.NotFound();
    
    return Results.Ok(patient);
})
.WithName("GetPatientById")
.WithOpenApi(operation => {
    operation.Summary = "Get Patient by ID";
    operation.Description = "Retrieves a specific patient by their unique id.";
    return operation;
});

/// Search Patients by GUID, First Name, or Last Name ///
app.MapGet("/patients/search", (string q, PatientService patientService) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest("Search term is required");
        
    var patients = patientService.SearchPatients(q);
    return patients.Any() ? Results.Ok(patients) : Results.NotFound();
})
.WithName("SearchPatients")
.WithDescription("Search for patients by ID, first name, or last name")
.WithOpenApi(operation => {
    operation.Summary = "Search Patients";
    operation.Description = "Search for patients using ID, first name, or last name";
    operation.Parameters[0].Description = "Search term (ID, first name, or last name)";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Management" } };
    return operation;
});

/// Add New Patient ///
app.MapPost("/patients", (Patient patient, PatientService patientService) =>
{
    var newPatient = patientService.AddPatient(patient);
    return Results.Created($"/patients/{newPatient.Id}", newPatient);
})
.WithName("AddPatient")
.WithOpenApi(operation => {
    operation.Summary = "Add New Patient";
    operation.Description = "Adds a new patient with GUID automatically generated if not provided.";
    return operation;
});

/// Update Patient ///
app.MapPut("/patients/{id}", (Guid id, Patient patient, PatientService patientService) =>
{
    if (id != patient.Id)
        return Results.BadRequest("ID mismatch");
        
    var updatedPatient = patientService.UpdatePatient(patient);
    if (updatedPatient == null)
        return Results.NotFound();
        
    return Results.Ok(updatedPatient);
})
.WithName("UpdatePatient")
.WithOpenApi(operation => {
    operation.Summary = "Update Patient.";
    operation.Description = "Updates a patient with new information.";
    return operation;
});

/// Delete Patient ///
app.MapDelete("/patients/{id}", (Guid id, PatientService patientService) =>
{
    var result = patientService.DeletePatient(id);
    if (!result)
        return Results.NotFound();
        
    return Results.NoContent();
})
.WithName("DeletePatient")
.WithOpenApi(operation => {
    operation.Summary = "Delete Patient.";
    operation.Description = "Deletes a patient from databse.";
    return operation;
});

app.Run();