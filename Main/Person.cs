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

        public string ChangeAttribute(int i, Faker faker)
        {
            if (faker == null) throw new ArgumentNullException(nameof(faker));

            switch (i)
            {
                case 0:
                    {
                        var old = Title;
                        var newTitle = faker.Name.Prefix();
                        while (string.Equals(newTitle, old, StringComparison.Ordinal))
                        {
                            newTitle = faker.Name.Prefix();
                        }
                        Title = newTitle;
                        Console.WriteLine($"Title changed: '{old}' -> '{Title}'");
                        return Title;
                    }

                case 1:
                    {
                        var old = FirstName;
                        var newFirst = faker.Name.FirstName();
                        while (string.Equals(newFirst, old, StringComparison.Ordinal))
                        {
                            newFirst = faker.Name.FirstName();
                        }
                        FirstName = newFirst;
                        Console.WriteLine($"FirstName changed: '{old}' -> '{FirstName}'");
                        return FirstName;
                    }

                case 2:
                    {
                        var old = LastName;
                        var newLast = faker.Name.LastName();
                        while (string.Equals(newLast, old, StringComparison.Ordinal))
                        {
                            newLast = faker.Name.LastName();
                        }
                        LastName = newLast;
                        Console.WriteLine($"LastName changed: '{old}' -> '{LastName}'");
                        return LastName;
                    }

                case 3:
                    {
                        var old = Email;
                        var newEmail = faker.Internet.Email();
                        while (string.Equals(newEmail, old, StringComparison.OrdinalIgnoreCase))
                        {
                            newEmail = faker.Internet.Email();
                        }
                        Email = newEmail;
                        Console.WriteLine($"Email changed: '{old}' -> '{Email}'");
                        return Email;
                    }

                case 4:
                    {
                        var old = Phone;
                        var newPhone = faker.Phone.PhoneNumber();
                        while (string.Equals(newPhone, old, StringComparison.Ordinal))
                        {
                            newPhone = faker.Phone.PhoneNumber();
                        }
                        Phone = newPhone;
                        Console.WriteLine($"Phone changed: '{old}' -> '{Phone}'");
                        return Phone;
                    }

                case 5:
                    {
                        var old = Gender;
                        var newGender = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        while (string.Equals(newGender, old, StringComparison.Ordinal))
                        {
                            newGender = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        }
                        Gender = newGender;
                        Console.WriteLine($"Gender changed: '{old}' -> '{Gender}'");
                        return Gender;
                    }

                case 6:
                    {
                        var old = Age;
                        var newAge = faker.Random.Int(18, 80);
                        while (newAge == old)
                        {
                            newAge = faker.Random.Int(18, 80);
                        }
                        Age = newAge;
                        Console.WriteLine($"Age changed: '{old}' -> '{Age}'");
                        return Age.ToString();
                    }

                case 7:
                    {
                        var old = Company;
                        var newCompany = faker.Company.CompanyName();
                        while (string.Equals(newCompany, old, StringComparison.Ordinal))
                        {
                            newCompany = faker.Company.CompanyName();
                        }
                        Company = newCompany;
                        Console.WriteLine($"Company changed: '{old}' -> '{Company}'");
                        return Company;
                    }

                case 8:
                    {
                        var old = JobTitle;
                        var newJob = faker.Name.JobTitle();
                        while (string.Equals(newJob, old, StringComparison.Ordinal))
                        {
                            newJob = faker.Name.JobTitle();
                        }
                        JobTitle = newJob;
                        Console.WriteLine($"JobTitle changed: '{old}' -> '{JobTitle}'");
                        return JobTitle;
                    }

                case 9:
                    {
                        var old = CreditCardNumber;
                        var newCard = faker.Finance.CreditCardNumber();
                        while (string.Equals(newCard, old, StringComparison.Ordinal))
                        {
                            newCard = faker.Finance.CreditCardNumber();
                        }
                        CreditCardNumber = newCard;
                        Console.WriteLine($"CreditCardNumber changed: '{old}' -> '{CreditCardNumber}'");
                        return CreditCardNumber;
                    }

                case 10:
                    {
                        var old = Street;
                        var newStreet = faker.Address.StreetName();
                        while (string.Equals(newStreet, old, StringComparison.Ordinal))
                        {
                            newStreet = faker.Address.StreetName();
                        }
                        Street = newStreet;
                        Console.WriteLine($"Street changed: '{old}' -> '{Street}'");
                        return Street;
                    }

                case 11:
                    {
                        var old = StreetNumber;
                        var newStreetNumber = faker.Address.SecondaryAddress();
                        while (string.Equals(newStreetNumber, old, StringComparison.Ordinal))
                        {
                            newStreetNumber = faker.Address.SecondaryAddress();
                        }
                        StreetNumber = newStreetNumber;
                        Console.WriteLine($"StreetNumber changed: '{old}' -> '{StreetNumber}'");
                        return StreetNumber;
                    }

                case 12:
                    {
                        var old = City;
                        var newCity = faker.Address.City();
                        while (string.Equals(newCity, old, StringComparison.Ordinal))
                        {
                            newCity = faker.Address.City();
                        }
                        City = newCity;
                        Console.WriteLine($"City changed: '{old}' -> '{City}'");
                        return City;
                    }

                case 13:
                    {
                        var old = County;
                        var newCounty = faker.Address.County();
                        while (string.Equals(newCounty, old, StringComparison.Ordinal))
                        {
                            newCounty = faker.Address.County();
                        }
                        County = newCounty;
                        Console.WriteLine($"County changed: '{old}' -> '{County}'");
                        return County;
                    }

                case 14:
                    {
                        var old = State;
                        var newState = faker.Address.State();
                        while (string.Equals(newState, old, StringComparison.Ordinal))
                        {
                            newState = faker.Address.State();
                        }
                        State = newState;
                        Console.WriteLine($"State changed: '{old}' -> '{State}'");
                        return State;
                    }

                case 15:
                    {
                        var old = ZipCode;
                        var newZip = faker.Address.ZipCode();
                        while (string.Equals(newZip, old, StringComparison.Ordinal))
                        {
                            newZip = faker.Address.ZipCode();
                        }
                        ZipCode = newZip;
                        Console.WriteLine($"ZipCode changed: '{old}' -> '{ZipCode}'");
                        return ZipCode;
                    }

                case 16:
                    {
                        var old = Country;
                        var newCountry = faker.Address.Country();
                        while (string.Equals(newCountry, old, StringComparison.Ordinal))
                        {
                            newCountry = faker.Address.Country();
                        }
                        Country = newCountry;
                        Console.WriteLine($"Country changed: '{old}' -> '{Country}'");
                        return Country;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }

        internal void SetAttribute(int i, string value)
        {
            switch (i)
            {
                case 0:
                    {
                        var old = Title;
                        Title = value;
                        Console.WriteLine($"Title set: '{old}' -> '{Title}'");
                        break;
                    }
                case 1:
                    {
                        var old = FirstName;
                        FirstName = value;
                        Console.WriteLine($"FirstName set: '{old}' -> '{FirstName}'");
                        break;
                    }
                case 2:
                    {
                        var old = LastName;
                        LastName = value;
                        Console.WriteLine($"LastName set: '{old}' -> '{LastName}'");
                        break;
                    }
                case 3:
                    {
                        var old = Email;
                        Email = value;
                        Console.WriteLine($"Email set: '{old}' -> '{Email}'");
                        break;
                    }
                case 4:
                    {
                        var old = Phone;
                        Phone = value;
                        Console.WriteLine($"Phone set: '{old}' -> '{Phone}'");
                        break;
                    }
                case 5:
                    {
                        var old = Gender;
                        Gender = value;
                        Console.WriteLine($"Gender set: '{old}' -> '{Gender}'");
                        break;
                    }
                case 6:
                    {
                        var old = Age;
                        if (!int.TryParse(value, out var parsedAge))
                            throw new FormatException($"Cannot parse Age from '{value}'.");
                        Age = parsedAge;
                        Console.WriteLine($"Age set: '{old}' -> '{Age}'");
                        break;
                    }
                case 7:
                    {
                        var old = Company;
                        Company = value;
                        Console.WriteLine($"Company set: '{old}' -> '{Company}'");
                        break;
                    }
                case 8:
                    {
                        var old = JobTitle;
                        JobTitle = value;
                        Console.WriteLine($"JobTitle set: '{old}' -> '{JobTitle}'");
                        break;
                    }
                case 9:
                    {
                        var old = CreditCardNumber;
                        CreditCardNumber = value;
                        Console.WriteLine($"CreditCardNumber set: '{old}' -> '{CreditCardNumber}'");
                        break;
                    }
                case 10:
                    {
                        var old = Street;
                        Street = value;
                        Console.WriteLine($"Street set: '{old}' -> '{Street}'");
                        break;
                    }
                case 11:
                    {
                        var old = StreetNumber;
                        StreetNumber = value;
                        Console.WriteLine($"StreetNumber set: '{old}' -> '{StreetNumber}'");
                        break;
                    }
                case 12:
                    {
                        var old = City;
                        City = value;
                        Console.WriteLine($"City set: '{old}' -> '{City}'");
                        break;
                    }
                case 13:
                    {
                        var old = County;
                        County = value;
                        Console.WriteLine($"County set: '{old}' -> '{County}'");
                        break;
                    }
                case 14:
                    {
                        var old = State;
                        State = value;
                        Console.WriteLine($"State set: '{old}' -> '{State}'");
                        break;
                    }
                case 15:
                    {
                        var old = ZipCode;
                        ZipCode = value;
                        Console.WriteLine($"ZipCode set: '{old}' -> '{ZipCode}'");
                        break;
                    }
                case 16:
                    {
                        var old = Country;
                        Country = value;
                        Console.WriteLine($"Country set: '{old}' -> '{Country}'");
                        break;
                    }
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

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"Title: {Title}",
                $"FirstName: {FirstName}",
                $"LastName: {LastName}",
                $"Email: {Email}",
                $"Phone: {Phone}",
                $"Gender: {Gender}",
                $"Age: {Age}",
                $"Company: {Company}",
                $"JobTitle: {JobTitle}",
                $"CreditCardNumber: {CreditCardNumber}",
                $"Street: {Street}",
                $"StreetNumber: {StreetNumber}",
                $"City: {City}",
                $"County: {County}",
                $"State: {State}",
                $"ZipCode: {ZipCode}",
                $"Country: {Country}"
            });
        }
    }
}

