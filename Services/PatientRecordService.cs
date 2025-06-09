using DentalPatientApp.Data;
using DentalPatientApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DentalPatientApp.Services;

/// <summary>
/// Service for managing dental patient records stored in PostgreSQL.
/// Provides methods for creating, retrieving, updating, and deleting dental records,
/// as well as automatic initialization of sample data.
/// </summary>

public class PatientRecordService
{
    private readonly DentalDbContext _context;
    private readonly PatientService _patientService;

   
    public PatientRecordService(DentalDbContext context, PatientService patientService)
    {
        _context = context; // database context for accessing PostgreSQL
        _patientService = patientService; // service for accessing patient information from LiteDB.

        // Check if we need to initialize sample data (aka, first run)
        try
        {
            Console.WriteLine("About to initialize sample data...");
            InitializeSampleDataAsync().Wait();  
            Console.WriteLine("Sample data initialization complete or skipped");
        }
        // Unlikely to need this as one successful run should be enough, but good "just in case"
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR initializing sample data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// Function for automatically initializing sample data if the database is empty.
    private async Task InitializeSampleDataAsync()
    {
        // Skip if we already have records
        bool hasRecords = await _context.PatientRecords.AnyAsync();
        Console.WriteLine($"Database already has records: {hasRecords}");
        if (hasRecords)
            return;

        // Get all patients from LiteDB. We need to be able to properly vet whether a patient exists before giving them a record.
        var patients = _patientService.GetAllPatients().ToList();
        Console.WriteLine($"Found {patients.Count} patients in LiteDB");
        if (!patients.Any())
            return;

        // We need random number gen for sample data generation
        var random = new Random();

        // Common dental procedures, dentists, treatments
        var procedures = new[] { "Regular Checkup", "Teeth Cleaning", "Fluoride Treatment",
            "Dental X-Ray", "Cavity Filling", "Root Canal", "Crown Placement", "Bridge Work",
            "Tooth Extraction", "Wisdom Tooth Removal", "Gum Treatment", "Teeth Whitening" };

        var dentists = new[] { "Dr. Sarah Johnson", "Dr. Michael Chen", "Dr. Emily Rodriguez",
            "Dr. David Kim", "Dr. Lisa Patel", "Dr. Robert Williams" };

        var treatments = new[] { "Complete cleaning with fluoride application",
            "Filling applied to affected area", "Pain management and antibiotics prescribed",
            "Crown fitted and adjusted", "X-rays taken of full mouth", "Root canal performed on tooth #" };

        int successCount = 0;
        int failCount = 0;

        // Process patients in smaller batches (no more than 10 patients at a time)
        foreach (var patientBatch in patients.Chunk(10))
        {
            // Create a smaller batch of records
            var batchRecords = new List<PatientRecord>();

            foreach (var patient in patientBatch)
            {
                // Each patient gets 1-3 records (smaller number for testing)
                int recordCount = random.Next(1, 4);

                for (int i = 0; i < recordCount; i++)
                {
                    // Create a DateTime and explicitly convert it to UTC.
                    var localDate = DateTime.Now.AddDays(-random.Next(1, 1095));
                    var recordDate = DateTime.SpecifyKind(localDate, DateTimeKind.Utc);

                    var toothNum = random.Next(1, 32);

                    try
                    {
                        var record = new PatientRecord
                        {
                            PatientId = patient.Id,
                            RecordDate = DateTime.UtcNow.AddDays(-random.Next(1, 1095)), // Create directly as UTC
                            RecordType = procedures[random.Next(procedures.Length)],
                            Description = $"Patient visit on {recordDate:MM/dd/yyyy}",
                            Treatment = $"{treatments[random.Next(treatments.Length)]} {(random.Next(0, 2) == 0 ? toothNum.ToString() : "")}",
                            Diagnosis = i % 3 == 0 ? "Healthy teeth and gums" :
                                       i % 3 == 1 ? $"Small cavity on tooth #{toothNum}" :
                                       "Mild gingivitis",
                            Prescription = i % 4 == 0 ? "None" :
                                          i % 4 == 1 ? "Antibiotics for 7 days" :
                                          i % 4 == 2 ? "Pain medication as needed" :
                                          "Medicated mouth rinse twice daily",
                            Notes = $"Patient {(random.Next(0, 2) == 0 ? "reported" : "did not report")} sensitivity.",
                            DentistName = dentists[random.Next(dentists.Length)]
                        };

                        batchRecords.Add(record);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating record: {ex.Message}");
                        failCount++;
                    }
                }
            }

            try
            {
                // Add the smaller batch
                await _context.PatientRecords.AddRangeAsync(batchRecords);
                await _context.SaveChangesAsync();
                successCount += batchRecords.Count;
                Console.WriteLine($"Added batch of {batchRecords.Count} records successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add batch: {ex.Message}");

                // Fall back to adding records one by one if batch fails
                foreach (var record in batchRecords)
                {
                    try
                    {
                        _context.PatientRecords.Add(record);
                        await _context.SaveChangesAsync();
                        successCount++;
                    }
                    catch
                    {
                        failCount++;
                    }
                }
            }
        }

        Console.WriteLine($"Added {successCount} sample dental records successfully, {failCount} failed");
    }

    //////////////////////////////
    /// Patient Record Methods ///
    //////////////////////////////
    
    /// Add new patient record to the database.
    /// Verifies patient exists before adding the record and ensures dates are in UTC format.
    public async Task<PatientRecord> AddRecordAsync(PatientRecord record)
    {
        // Verify that the patient exists before adding a record
        var patient = _patientService.GetPatient(record.PatientId);
        if (patient == null)
        {
            throw new KeyNotFoundException($"Patient with ID {record.PatientId} not found");
        }

        // Ensure DateTime is UTC
        if (record.RecordDate.Kind != DateTimeKind.Utc)
        {
            record.RecordDate = DateTime.SpecifyKind(record.RecordDate, DateTimeKind.Utc);
        }

        await _context.PatientRecords.AddAsync(record);
        await _context.SaveChangesAsync();
        return record;
    }

    /// Get all patient records for a specific patient, ordered by record date descending. ///
    public async Task<List<PatientRecord>> GetPatientRecordsAsync(Guid patientId)
    {
        return await _context.PatientRecords
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync();
    }

    /// Get a specific patient record by its ID. ///
    public async Task<PatientRecord?> GetRecordAsync(int recordId)
    {
        return await _context.PatientRecords.FindAsync(recordId);
    }

    /// Update an existing patient record. ///
    public async Task<PatientRecord?> UpdateRecordAsync(PatientRecord record)
    {
        var existingRecord = await _context.PatientRecords.FindAsync(record.Id);
        if (existingRecord == null)
        {
            return null;
        }

        // Update fields
        existingRecord.RecordType = record.RecordType;
        existingRecord.Description = record.Description;
        existingRecord.Treatment = record.Treatment;
        existingRecord.Diagnosis = record.Diagnosis;
        existingRecord.Prescription = record.Prescription;
        existingRecord.Notes = record.Notes;
        existingRecord.DentistName = record.DentistName;

        await _context.SaveChangesAsync();
        return existingRecord;
    }

    /// Delete a patient record by its ID. Returns true if deleted, false if not found. ///
    public async Task<bool> DeleteRecordAsync(int recordId)
    {
        var record = await _context.PatientRecords.FindAsync(recordId);
        if (record == null)
        {
            return false;
        }

        _context.PatientRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    /// Get patient records with details from both PostgreSQL and LiteDB.
    public async Task<IEnumerable<dynamic>> GetPatientRecordsWithDetailsAsync(Guid patientId)
    {
        // Get patient from LiteDB
        var patient = _patientService.GetPatient(patientId);
        if (patient == null)
            return Enumerable.Empty<dynamic>();

        // Get records from PostgreSQL
        var records = await _context.PatientRecords
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync();

        // Combine the data on-the-fly
        return records.Select(record => new
        {
            Record = record,
            Patient = new
            {
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth
            }
        });
    }
}