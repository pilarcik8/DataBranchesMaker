using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using Bogus;

namespace TestKniznice
{
    public class Person
    {
        public string Title { get; set; } //0
        public string FirstName { get; set; } //1
        public string LastName { get; set; } //2

        public string Email { get; set; } //3
        public string Phone { get; set; } //4

        public string Gender { get; set; }  //5
        public int Age { get; set; } //6
        public string Company { get; set; } //7
        public string JobTitle { get; set; } //8

        public string CreditCardNumber { get; set; } //9
        public string Street { get; set; } //10
        public string StreetNumber { get; set; } //11
        public string City { get; set; } //12
        public string County { get; set; } //13
        public string State { get; set; } //14
        public string ZipCode { get; set; } //15
        public string Country { get; set; } //16

        public Person()
        {
            Title = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Gender = string.Empty;
            Age = 0;
            Company = string.Empty;
            JobTitle = string.Empty;
            CreditCardNumber = string.Empty;
            Street = string.Empty;
            StreetNumber = string.Empty;
            City = string.Empty;
            County = string.Empty;
            State = string.Empty;
            ZipCode = string.Empty;
            Country = string.Empty;
        }

        // Generic getter by index (uses switch). Index mapping listed below.
        public T GetAtribute<T>(int index)
        {
            object? value = index switch
            {
                0 => Title,
                1 => FirstName,
                2 => LastName,
                3 => Email,
                4 => Phone,
                5 => Gender,
                6 => Age,
                7 => Company,
                8 => JobTitle,
                9 => CreditCardNumber,
                10 => Street,
                11 => StreetNumber,
                12 => City,
                13 => County,
                14 => State,
                15 => ZipCode,
                16 => Country,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Invalid attribute index")
            };

            if (value == null)
                return default!;

            if (value is T t)
                return t;

            try
            {
                // attempt conversion for simple types (e.g., int -> string or string -> int)
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert attribute at index {index} (type {value.GetType()}) to {typeof(T)}.", ex);
            }
        }

        public Type GetTypeOfAtribute(int index)
        {
            return index switch
            {
                0 => typeof(string),
                1 => typeof(string),
                2 => typeof(string),
                3 => typeof(string),
                4 => typeof(string),
                5 => typeof(string),
                6 => typeof(int),
                7 => typeof(string),
                8 => typeof(string),
                9 => typeof(string),
                10 => typeof(string),
                11 => typeof(string),
                12 => typeof(string),
                13 => typeof(string),
                14 => typeof(string),
                15 => typeof(string),
                16 => typeof(string),
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Invalid attribute index")
            };
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"Firstname: {FirstName}, Lastname: {LastName}",
                $"Email: {Email}, Phone: {Phone}",
                $"Age: {Age}, Gender: {Gender}",
                $"Company: {Company}, Job: {JobTitle}",
                $"Card: {CreditCardNumber}",
                $"Adress: {Street} {StreetNumber}, {City}, {Country}"
            });
        }

        internal void ChangeAttribute(int i, Faker faker)
        {
            if (faker == null) throw new ArgumentNullException(nameof(faker));

            switch (i)
            {
                case 0:
                    Title = faker.Name.Prefix();
                    break;
                case 1:
                    FirstName = faker.Name.FirstName();
                    break;
                case 2:
                    LastName = faker.Name.LastName();
                    break;
                case 3:
                    Email = faker.Internet.Email();
                    break;
                case 4:
                    Phone = faker.Phone.PhoneNumber();
                    break;
                case 5:
                    Gender = faker.PickRandom(new[] { "Male", "Female", "Other" });
                    break;
                case 6:
                    Age = faker.Random.Int(18, 80);
                    break;
                case 7:
                    Company = faker.Company.CompanyName();
                    break;
                case 8:
                    JobTitle = faker.Name.JobTitle();
                    break;
                case 9:
                    CreditCardNumber = faker.Finance.CreditCardNumber();
                    break;
                case 10:
                    Street = faker.Address.StreetName();
                    break;
                case 11:
                    StreetNumber = faker.Address.SecondaryAddress();
                    break;
                case 12:
                    City = faker.Address.City();
                    break;
                case 13:
                    County = faker.Address.County();
                    break;
                case 14:
                    State = faker.Address.State();
                    break;
                case 15:
                    ZipCode = faker.Address.ZipCode();
                    break;
                case 16:
                    Country = faker.Address.Country();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }
    }
}

