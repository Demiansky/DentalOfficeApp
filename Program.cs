using DentalPatientApp.Models;
using DentalPatientApp.Services;
using Microsoft.OpenApi.Models;
using DentalPatientApp.Data;
using Microsoft.EntityFrameworkCore;

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

// Add PostgreSQL connection
builder.Services.AddDbContext<DentalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the PatientRecordService
builder.Services.AddScoped<PatientRecordService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        Console.WriteLine("Explicitly requesting PatientRecordService...");
        var recordService = scope.ServiceProvider.GetRequiredService<PatientRecordService>();
        Console.WriteLine("PatientRecordService created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating PatientRecordService: {ex.Message}");
    }
}

// Configure the HTTP request pipeline. //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

///////////////////////////////////
// Patient API endpoints (LiteDB //
///////////////////////////////////

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
.WithOpenApi(operation =>
{
    operation.Summary = "Delete Patient.";
    operation.Description = "Deletes a patient from databse.";
    return operation;
});

////////////////////////////////////////////////
/// PostgreSQL Patient Records API Endpoints ///
////////////////////////////////////////////////

/// Get all records for a patient ///
app.MapGet("/patients/{patientId}/records", async (Guid patientId, PatientRecordService recordService) =>
{
    var records = await recordService.GetPatientRecordsAsync(patientId);
    return records.Any() ? Results.Ok(records) : Results.NotFound();
})
.WithName("GetPatientRecords")
.WithOpenApi(operation => {
    operation.Summary = "Get Patient Records";
    operation.Description = "Retrieves all dental records for a specific patient";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

/// Get a specific record ///
app.MapGet("/records/{recordId}", async (int recordId, PatientRecordService recordService) =>
{
    var record = await recordService.GetRecordAsync(recordId);
    return record != null ? Results.Ok(record) : Results.NotFound();
})
.WithName("GetRecordById")
.WithOpenApi(operation => {
    operation.Summary = "Get Record by ID";
    operation.Description = "Retrieves a specific dental record by its ID";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

/// Add a new record ///
app.MapPost("/patients/{patientId}/records", async (Guid patientId, PatientRecord record, PatientRecordService recordService) =>
{
    // Set the patient ID from the route
    record.PatientId = patientId;
    
    try
    {
        var newRecord = await recordService.AddRecordAsync(record);
        return Results.Created($"/records/{newRecord.Id}", newRecord);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
})
.WithName("AddPatientRecord")
.WithOpenApi(operation => {
    operation.Summary = "Add Patient Record";
    operation.Description = "Creates a new dental record for a specific patient";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

/// Update a record ///
app.MapPut("/records/{recordId}", async (int recordId, PatientRecord record, PatientRecordService recordService) =>
{
    if (recordId != record.Id)
        return Results.BadRequest("ID mismatch");
        
    var updatedRecord = await recordService.UpdateRecordAsync(record);
    if (updatedRecord == null)
        return Results.NotFound();
        
    return Results.Ok(updatedRecord);
})
.WithName("UpdateRecord")
.WithOpenApi(operation => {
    operation.Summary = "Update Record";
    operation.Description = "Updates an existing dental record";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

/// Delete a record ///
app.MapDelete("/records/{recordId}", async (int recordId, PatientRecordService recordService) =>
{
    var result = await recordService.DeleteRecordAsync(recordId);
    if (!result)
        return Results.NotFound();
        
    return Results.NoContent();
})
.WithName("DeleteRecord")
.WithOpenApi(operation => {
    operation.Summary = "Delete Record";
    operation.Description = "Permanently removes a dental record";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

/// Get patient records with details ///
app.MapGet("/patients/{patientId}/records/details", async (Guid patientId, PatientRecordService recordService) =>
{
    var records = await recordService.GetPatientRecordsWithDetailsAsync(patientId);
    return records.Any() ? Results.Ok(records) : Results.NotFound();
})
.WithName("GetPatientRecordsWithDetails")
.WithOpenApi(operation => {
    operation.Summary = "Get Patient Records with Details";
    operation.Description = "Retrieves dental records with patient information for a specific patient";
    operation.Tags = new List<OpenApiTag> { new() { Name = "Patient Records" } };
    return operation;
});

app.Run();