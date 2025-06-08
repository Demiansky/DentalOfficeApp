using DentalPatientApp.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DentalPatientApp.Services;

public class PatientService : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Patient> _patients;
    
    public PatientService(IConfiguration configuration)
    {
        var dbPath = configuration["DatabaseSettings:LiteDbPath"] ?? "DentalPatients.db";
        _database = new LiteDatabase(dbPath);
        _patients = _database.GetCollection<Patient>("patients");
        
        // Create index for faster lookups
        _patients.EnsureIndex(x => x.Id);
        
        // Add sample data if collection is empty
        if (_patients.Count() == 0)
        {
            InitializeSampleData();
        }
    }
    
    private void InitializeSampleData()
    {
        // Generate 50 patients with realistic data
        var random = new Random();
        var firstNames = new[] { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth", 
                              "David", "Susan", "Richard", "Jessica", "Joseph", "Sarah", "Thomas", "Karen", "Charles", "Nancy", 
                              "Christopher", "Lisa", "Daniel", "Margaret", "Matthew", "Betty", "Anthony", "Sandra", "Mark", "Ashley",
                              "Donald", "Dorothy", "Steven", "Kimberly", "Andrew", "Emily", "Paul", "Donna", "Joshua", "Michelle",
                              "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Melissa", "George", "Deborah", "Timothy", "Stephanie" };
                              
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor",
                             "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin", "Thompson", "Garcia", "Martinez", "Robinson",
                             "Clark", "Rodriguez", "Lewis", "Lee", "Walker", "Hall", "Allen", "Young", "Hernandez", "King",
                             "Wright", "Lopez", "Hill", "Scott", "Green", "Adams", "Baker", "Gonzalez", "Nelson", "Carter",
                             "Mitchell", "Perez", "Roberts", "Turner", "Phillips", "Campbell", "Parker", "Evans", "Edwards", "Collins" };
                             
        var streetNames = new[] { "Main", "Oak", "Maple", "Cedar", "Pine", "Elm", "Washington", "Lake", "Hill", "First",
                               "Park", "River", "Meadow", "Forest", "High", "Valley", "Spring", "Church", "Mill", "Bridge",
                               "North", "South", "East", "West", "Ridge", "Center", "Water", "Sunset", "Highland", "Union",
                               "School", "Willow", "Jefferson", "College", "Pleasant", "Adams", "Madison", "Franklin", "Dogwood", "Hickory",
                               "Fairview", "Sycamore", "Central", "Laurel", "Spruce", "Poplar", "Broadway", "Cherry", "Walnut", "Beach" };
                               
        var streetTypes = new[] { "St", "Ave", "Rd", "Blvd", "Ln", "Dr", "Way", "Pl", "Ct" };
        
        var cities = new[] { "Springfield", "Georgetown", "Franklin", "Greenville", "Bristol", "Clinton", "Kingston", "Fairview", "Salem", "Madison",
                          "Oxford", "Arlington", "Burlington", "Winchester", "Milford", "Newport", "Auburn", "Ashland", "Dover", "Hudson",
                          "Manchester", "Centerville", "Dayton", "Cleveland", "Oakland", "Riverside", "Lexington", "Jackson", "Denver", "Phoenix",
                          "Portland", "Seattle", "Chicago", "Boston", "Dallas", "Austin", "Nashville", "Orlando", "Miami", "Atlanta",
                          "Richmond", "Charleston", "Columbia", "Savannah", "Raleigh", "Charlotte", "Memphis", "Baltimore", "Pittsburgh", "Detroit" };
        
        var states = new[] { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA", 
                          "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
                          "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
                          "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
                          "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY" };
                          
        var dentalConditions = new[] { "Needs fluoride treatment.", "Has dental anxiety.", "Regular check-up patient.", "Teeth whitening candidate.", 
                                   "Root canal completed on #16.", "Dental implant on #7.", "Requires orthodontic evaluation.", "Gum disease treatment ongoing.", 
                                   "High cavity risk.", "Sensitive teeth.", "Needs crown replacement.", "Denture adjustment required.", "Wisdom teeth extracted.",
                                   "Requires night guard for bruxism.", "Periodontal maintenance.", "Interested in veneers.", "History of TMJ issues.",
                                   "Requires special care due to diabetes.", "Former smoker with oral health concerns.", "Interested in clear aligners.",
                                   "Regular flossing required.", "Family history of oral cancer.", "Good oral hygiene habits.", "Recovering from oral surgery.",
                                   "Requires assistance with brushing technique." };
        
        for (int i = 1; i <= 50; i++)
        {
            // Calculate a random birth date between 18 and 90 years ago
            int age = random.Next(18, 90);
            var birthDate = DateTime.Now.AddYears(-age).AddDays(random.Next(365));
            
            // Calculate last appointment (between 1 day and 2 years ago)
            var lastAppointment = DateTime.Now.AddDays(-random.Next(1, 730));
            
            // 70% chance of having a next appointment
            DateTime? nextAppointment = null;
            if (random.NextDouble() < 0.7)
            {
                // Next appointment within the next year
                nextAppointment = DateTime.Now.AddDays(random.Next(1, 365));
            }
            
            // Construct address
            string address = $"{random.Next(1, 9999)} {streetNames[random.Next(streetNames.Length)]} {streetTypes[random.Next(streetTypes.Length)]}, " +
                          $"{cities[random.Next(cities.Length)]}, {states[random.Next(states.Length)]} {random.Next(10000, 99999)}";
            
            // Construct phone number
            string phone = $"{random.Next(200, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";
            
            // Create and insert patient with GUID
            _patients.Insert(new Patient
            {
                // Guid.NewGuid() will be called automatically by the constructor
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                DateOfBirth = birthDate,
                Email = $"{firstNames[random.Next(firstNames.Length)].ToLower()}.{lastNames[random.Next(lastNames.Length)].ToLower()}@example.com",
                PhoneNumber = phone,
                Address = address,
                LastAppointment = lastAppointment,
                NextAppointment = nextAppointment,
                Notes = dentalConditions[random.Next(dentalConditions.Length)]
            });
        }
    }
    
    public IEnumerable<Patient> GetAllPatients() => _patients.FindAll().ToList();

    public IEnumerable<Patient> SearchPatients(string searchTerm)
{
    // If searchTerm is a valid GUID, search by ID
    if (Guid.TryParse(searchTerm, out Guid id))
    {
        var patient = _patients.FindById(id);
        return patient != null ? new List<Patient> { patient } : Enumerable.Empty<Patient>();
    }
    
    // Otherwise, search by name (case insensitive)
    searchTerm = searchTerm.ToLowerInvariant();
    
    return _patients.FindAll()
        .Where(p => 
            p.FirstName.ToLowerInvariant().Contains(searchTerm) || 
            p.LastName.ToLowerInvariant().Contains(searchTerm) ||
            $"{p.FirstName} {p.LastName}".ToLowerInvariant().Contains(searchTerm)
        )
        .ToList();
}

    public Patient? GetPatient(Guid id) => _patients.FindById(id);

    public Patient AddPatient(Patient patient)
    {
        // Ensure the patient has a GUID
        if (patient.Id == Guid.Empty)
        {
            patient.Id = Guid.NewGuid();
        }
        
        _patients.Insert(patient);
        return patient;
    }

    public Patient? UpdatePatient(Patient patient)
    {
        var exists = _patients.FindById(patient.Id) != null;
        if (!exists)
            return null;
            
        _patients.Update(patient);
        return patient;
    }

    public bool DeletePatient(Guid id)
    {
        return _patients.Delete(id);
    }
    
    public void Dispose()
    {
        _database?.Dispose();
    }
}