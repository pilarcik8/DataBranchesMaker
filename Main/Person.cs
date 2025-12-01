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
        public string? Title { get; set; } //0
        public string? FirstName { get; set; } //1
        public string? LastName { get; set; } //2

        public string? Email { get; set; } //3
        public string? Phone { get; set; } //4

        public string? Gender { get; set; }  //5
        public int? Age { get; set; } //6
        public string? Company { get; set; } //7
        public string? JobTitle { get; set; } //8

        public string? CreditCardNumber { get; set; } //9
        public string? Street { get; set; } //10
        public string? StreetNumber { get; set; } //11
        public string? City { get; set; } //12
        public string? County { get; set; } //13
        public string? State { get; set; } //14
        public string? ZipCode { get; set; } //15
        public string? Country { get; set; } //16

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

        public string ChangeAttribute(int i, Faker faker)
        {
            if (faker == null) throw new ArgumentNullException(nameof(faker));

            var old = GetAttribute(i);
            string newValue;

            switch (i)
            {
                case 0:
                    {
                        var newTitle = faker.Name.Prefix();
                        while (string.Equals(newTitle, old, StringComparison.Ordinal))
                        {
                            newTitle = faker.Name.Prefix();
                        }
                        Title = newTitle;
                        newValue = Title;
                        break;
                    }

                case 1:
                    {
                        var newFirst = faker.Name.FirstName();
                        while (string.Equals(newFirst, old, StringComparison.Ordinal))
                        {
                            newFirst = faker.Name.FirstName();
                        }
                        FirstName = newFirst;
                        newValue = FirstName;
                        break;
                    }

                case 2:
                    {
                        var newLast = faker.Name.LastName();
                        while (string.Equals(newLast, old, StringComparison.Ordinal))
                        {
                            newLast = faker.Name.LastName();
                        }
                        LastName = newLast;
                        newValue = LastName;
                        break;
                    }

                case 3:
                    {
                        var newEmail = faker.Internet.Email();
                        while (string.Equals(newEmail, old, StringComparison.OrdinalIgnoreCase))
                        {
                            newEmail = faker.Internet.Email();
                        }
                        Email = newEmail;
                        newValue = Email;
                        break;
                    }

                case 4:
                    {
                        var newPhone = faker.Phone.PhoneNumber();
                        while (string.Equals(newPhone, old, StringComparison.Ordinal))
                        {
                            newPhone = faker.Phone.PhoneNumber();
                        }
                        Phone = newPhone;
                        newValue = Phone;
                        break;
                    }

                case 5:
                    {
                        var newGender = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        while (string.Equals(newGender, old, StringComparison.Ordinal))
                        {
                            newGender = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        }
                        Gender = newGender;
                        newValue = Gender;
                        break;
                    }

                case 6:
                    {
                        var newAge = faker.Random.Int(18, 80);
                        while (string.Equals(newAge.ToString(), old, StringComparison.Ordinal))
                        {
                            newAge = faker.Random.Int(18, 80);
                        }
                        Age = newAge;
                        //change by nemal nikdy byť null
                        newValue = Age.ToString();
                        break;
                    }

                case 7:
                    {
                        var newCompany = faker.Company.CompanyName();
                        while (string.Equals(newCompany, old, StringComparison.Ordinal))
                        {
                            newCompany = faker.Company.CompanyName();
                        }
                        Company = newCompany;
                        newValue = Company;
                        break;
                    }

                case 8:
                    {
                        var newJob = faker.Name.JobTitle();
                        while (string.Equals(newJob, old, StringComparison.Ordinal))
                        {
                            newJob = faker.Name.JobTitle();
                        }
                        JobTitle = newJob;
                        newValue = JobTitle;
                        break;
                    }

                case 9:
                    {
                        var newCard = faker.Finance.CreditCardNumber();
                        while (string.Equals(newCard, old, StringComparison.Ordinal))
                        {
                            newCard = faker.Finance.CreditCardNumber();
                        }
                        CreditCardNumber = newCard;
                        newValue = CreditCardNumber;
                        break;
                    }

                case 10:
                    {
                        var newStreet = faker.Address.StreetName();
                        while (string.Equals(newStreet, old, StringComparison.Ordinal))
                        {
                            newStreet = faker.Address.StreetName();
                        }
                        Street = newStreet;
                        newValue = Street;
                        break;
                    }

                case 11:
                    {
                        var newStreetNumber = faker.Address.SecondaryAddress();
                        while (string.Equals(newStreetNumber, old, StringComparison.Ordinal))
                        {
                            newStreetNumber = faker.Address.SecondaryAddress();
                        }
                        StreetNumber = newStreetNumber;
                        newValue = StreetNumber;
                        break;
                    }

                case 12:
                    {
                        var newCity = faker.Address.City();
                        while (string.Equals(newCity, old, StringComparison.Ordinal))
                        {
                            newCity = faker.Address.City();
                        }
                        City = newCity;
                        newValue = City;
                        break;
                    }

                case 13:
                    {
                        var newCounty = faker.Address.County();
                        while (string.Equals(newCounty, old, StringComparison.Ordinal))
                        {
                            newCounty = faker.Address.County();
                        }
                        County = newCounty;
                        newValue = County;
                        break;
                    }

                case 14:
                    {
                        var newState = faker.Address.State();
                        while (string.Equals(newState, old, StringComparison.Ordinal))
                        {
                            newState = faker.Address.State();
                        }
                        State = newState;
                        newValue = State;
                        break;
                    }

                case 15:
                    {
                        var newZip = faker.Address.ZipCode();
                        while (string.Equals(newZip, old, StringComparison.Ordinal))
                        {
                            newZip = faker.Address.ZipCode();
                        }
                        ZipCode = newZip;
                        newValue = ZipCode;
                        break;
                    }

                case 16:
                    {
                        var newCountry = faker.Address.Country();
                        while (string.Equals(newCountry, old, StringComparison.Ordinal))
                        {
                            newCountry = faker.Address.Country();
                        }
                        Country = newCountry;
                        newValue = Country;
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }

            Console.WriteLine($"     Changed attribute: '{GetAttributeName(i)}' from '{old}' to '{newValue}'");

            // Neozaj by nemalo byt null
            return newValue;
        }

        public string? GetAttribute(int i)
        {
            switch (i)
            {
                case 0: return Title;
                case 1: return FirstName;
                case 2: return LastName;
                case 3: return Email;
                case 4: return Phone;
                case 5: return Gender;
                case 6: return Age?.ToString();
                case 7: return Company;
                case 8: return JobTitle;
                case 9: return CreditCardNumber;
                case 10: return Street;
                case 11: return StreetNumber;
                case 12: return City;
                case 13: return County;
                case 14: return State;
                case 15: return ZipCode;
                case 16: return Country;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }

        internal void RemoveAtribute(int i)
        {
            SetAttribute(i, null);
        }


        internal void SetAttribute(int i, string? value)
        {
            var old = GetAttribute(i);
            var name = GetAttributeName(i);

            switch (i)
            {
                case 0:
                    {
                        Title = value;
                        break;
                    }
                case 1:
                    {
                        FirstName = value;
                        break;
                    }
                case 2:
                    {
                        LastName = value;
                        break;
                    }
                case 3:
                    {
                        Email = value;
                        break;
                    }
                case 4:
                    {
                        Phone = value;
                        break;
                    }
                case 5:
                    {
                        Gender = value;
                        break;
                    }
                case 6:
                    {
                        if (value == null)
                        {
                            Age = null;
                            break;
                        }

                        if (!int.TryParse(value, out var parsedAge))
                            throw new FormatException($"Cannot parse Age from '{value}'.");
                        Age = parsedAge;
                        break;
                    }
                case 7:
                    {
                        Company = value;
                        break;
                    }
                case 8:
                    {
                        JobTitle = value;
                        break;
                    }
                case 9:
                    {
                        CreditCardNumber = value;
                        break;
                    }
                case 10:
                    {
                        Street = value;
                        break;
                    }
                case 11:
                    {
                        StreetNumber = value;
                        break;
                    }
                case 12:
                    {
                        City = value;
                        break;
                    }
                case 13:
                    {
                        County = value;
                        break;
                    }
                case 14:
                    {
                        State = value;
                        break;
                    }
                case 15:
                    {
                        ZipCode = value;
                        break;
                    }
                case 16:
                    {
                        Country = value;
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }

            //Console.WriteLine($"'{name}' set: '{old}' -> '{value}'");

        }

        public string GetAttributeName(int i)
        {
            switch (i)
            {
                case 0: return nameof(Title);
                case 1: return nameof(FirstName);
                case 2: return nameof(LastName);
                case 3: return nameof(Email);
                case 4: return nameof(Phone);
                case 5: return nameof(Gender);
                case 6: return nameof(Age);
                case 7: return nameof(Company);
                case 8: return nameof(JobTitle);
                case 9: return nameof(CreditCardNumber);
                case 10: return nameof(Street);
                case 11: return nameof(StreetNumber);
                case 12: return nameof(City);
                case 13: return nameof(County);
                case 14: return nameof(State);
                case 15: return nameof(ZipCode);
                case 16: return nameof(Country);
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }

        public Person Clone()
        {
            return new Person
            {
                Title = this.Title,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Email = this.Email,
                Phone = this.Phone,
                Gender = this.Gender,
                Age = this.Age,
                Company = this.Company,
                JobTitle = this.JobTitle,
                CreditCardNumber = this.CreditCardNumber,
                Street = this.Street,
                StreetNumber = this.StreetNumber,
                City = this.City,
                County = this.County,
                State = this.State,
                ZipCode = this.ZipCode,
                Country = this.Country
            };
        }
    }
}