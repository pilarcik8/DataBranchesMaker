using Bogus;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestKniznice
{
    // Cely Person.cs zalezi na tom aby sa nemenili tieto atributy a ich indexy
    // Kazdy druhy atribut je aby sa pocas runntimu dal "pridat" novy atribut (nastaveny na null == neexistuje pre xml)
    public class Person
    {
        private string? Iban;
        public string? Title { get; set; } //0
        /*---------------------------------------*/
        private string? FavouriteColor;
        public string? FirstName { get; set; } //1
        /*---------------------------------------*/
        public string? BitcoinAddress;
        public string? LastName { get; set; } //2
        /*---------------------------------------*/
        public string? EmailUserName;
        public string? Email { get; set; } //3
        /*---------------------------------------*/

        public string? Phone { get; set; } //4
        public string? PhoneExtension;
        /*---------------------------------------*/
        public string? FavouriteWord;
        public string? Gender { get; set; }  //5
        /*---------------------------------------*/
        public string? FavouriteMusicGenre;
        public int? Age { get; set; } //6
        /*---------------------------------------*/
        public string? CompanyCatchPhrase;
        public string? Company { get; set; } //7
        /*---------------------------------------*/
        public string? JobDescriptor;
        public string? JobTitle { get; set; } //8
        /*---------------------------------------*/
        public string? CreditAccount;
        public string? CreditCardNumber { get; set; } //9
        /*---------------------------------------*/
        public string? StreetSuffix;
        public string? Street { get; set; } //10
        /*---------------------------------------*/
        public int? BuildingNumber;
        public string? StreetNumber { get; set; }
        /*---------------------------------------*/
        public string? CityPrefix;
        public string? City { get; set; } //12
        /*---------------------------------------*/
        public string? CountyCode;
        public string? County { get; set; } //13
        /*---------------------------------------*/

        public string? State { get; set; } //14
        public string? StateAbbr;
        /*---------------------------------------*/
        public string? ZipPlus4;
        public string? ZipCode { get; set; } //15
        /*---------------------------------------*/
        public string? CountryCode;
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

            Iban = null;
            FavouriteColor = null;
            BitcoinAddress = null;
            EmailUserName = null;
            PhoneExtension = null;
            FavouriteWord = null;
            FavouriteMusicGenre = null;
            CompanyCatchPhrase = null;
            JobDescriptor = null;
            CreditAccount = null;
            StreetSuffix = null;
            BuildingNumber = null;
            CityPrefix = null;
            CountyCode = null;
            StateAbbr = null;
            ZipPlus4 = null;
            CountryCode = null;
        }

        public string AddAttribute(int i, Faker faker, string preGeneratedValue = "")
        {
            string value = "";
            string name = "";

            // Použi už predgenerovanú hodnotu, ak existuje
            bool hasPreGen = !string.IsNullOrWhiteSpace(preGeneratedValue);

            switch (i)
            {
                case 0:
                    value = hasPreGen ? preGeneratedValue : faker.Finance.Iban();
                    this.Iban = value;
                    name = nameof(Iban);
                    break;

                case 1:
                    value = hasPreGen ? preGeneratedValue : faker.Commerce.Color();
                    this.FavouriteColor = value;
                    name = nameof(FavouriteColor);
                    break;

                case 2:
                    value = hasPreGen ? preGeneratedValue : faker.Finance.BitcoinAddress();
                    this.BitcoinAddress = value;
                    name = nameof(BitcoinAddress);
                    break;

                case 3:
                    value = hasPreGen ? preGeneratedValue : faker.Internet.UserName();
                    this.EmailUserName = value;
                    name = nameof(EmailUserName);
                    break;

                case 4:
                    value = hasPreGen ? preGeneratedValue : faker.Random.AlphaNumeric(8);
                    this.PhoneExtension = value;
                    name = nameof(PhoneExtension);
                    break;

                case 5:
                    value = hasPreGen ? preGeneratedValue : faker.Random.Word();
                    this.FavouriteWord = value;
                    name = nameof(FavouriteWord);
                    break;

                case 6:
                    value = hasPreGen ? preGeneratedValue : faker.Music.Genre();
                    this.FavouriteMusicGenre = value;
                    name = nameof(FavouriteMusicGenre);
                    break;

                case 7:
                    value = hasPreGen ? preGeneratedValue : faker.Company.CatchPhrase();
                    this.CompanyCatchPhrase = value;
                    name = nameof(CompanyCatchPhrase);
                    break;

                case 8:
                    value = hasPreGen ? preGeneratedValue : faker.Hacker.Phrase();
                    this.JobDescriptor = value;
                    name = nameof(JobDescriptor);
                    break;

                case 9:
                    value = hasPreGen ? preGeneratedValue : faker.Finance.Account();
                    this.CreditAccount = value;
                    name = nameof(CreditAccount);
                    break;

                case 10:
                    value = hasPreGen ? preGeneratedValue : faker.Address.StreetSuffix();
                    this.StreetSuffix = value;
                    name = nameof(StreetSuffix);
                    break;

                case 11:
                    value = hasPreGen ? preGeneratedValue : faker.Address.BuildingNumber();
                    this.BuildingNumber = ParseNullableInt(value);
                    name = nameof(BuildingNumber);
                    break;

                case 12:
                    value = hasPreGen ? preGeneratedValue : faker.Address.CityPrefix();
                    this.CityPrefix = value;
                    name = nameof(CityPrefix);
                    break;

                case 13:
                    value = hasPreGen ? preGeneratedValue : faker.Random.AlphaNumeric(5);
                    this.CountyCode = value;
                    name = nameof(CountyCode);
                    break;

                case 14:
                    value = hasPreGen ? preGeneratedValue : faker.Address.StateAbbr();
                    this.StateAbbr = value;
                    name = nameof(StateAbbr);
                    break;

                case 15:
                    value = hasPreGen ? preGeneratedValue : faker.Random.Number(1000, 9999).ToString();
                    this.ZipPlus4 = value;
                    name = nameof(ZipPlus4);
                    break;

                case 16:
                    value = hasPreGen ? preGeneratedValue : faker.Address.CountryCode();
                    this.CountryCode = value;
                    name = nameof(CountryCode);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }

            var valueAndName = $"{value}|{name}";
            return valueAndName;
        }

        // Vrati hodnotu atributu po jeho zmene, aby sa dala nastavit aj do druhehej osoby
        public string ChangeAttribute(int i, Faker faker)
        {
            var old = GetAttribute(i);
            string newValue;

            switch (i)
            {
                case 0:
                    {
                        newValue = faker.Name.Prefix();
                        while (newValue == old)
                            newValue = faker.Name.Prefix();
                        break;
                    }

                case 1:
                    {
                        newValue = faker.Name.FirstName();
                        while (newValue == old)
                            newValue = faker.Name.FirstName();
                        break;
                    }

                case 2:
                    {
                        newValue = faker.Name.LastName();
                        while (newValue == old)
                            newValue = faker.Name.LastName();
                        break;
                    }

                case 3:
                    {
                        newValue = faker.Internet.Email();
                        while (string.Equals(newValue, old, StringComparison.OrdinalIgnoreCase))
                            newValue = faker.Internet.Email();
                        break;
                    }

                case 4:
                    {
                        newValue = faker.Phone.PhoneNumber();
                        while (newValue == old)
                            newValue = faker.Phone.PhoneNumber();
                        break;
                    }

                case 5:
                    {
                        newValue = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        while (newValue == old)
                            newValue = faker.PickRandom(new[] { "Male", "Female", "Other" });
                        break;
                    }

                case 6:
                    {
                        int newAge = faker.Random.Int(18, 80);
                        while (newAge.ToString() == old)
                            newAge = faker.Random.Int(18, 80);

                        newValue = newAge.ToString();
                        break;
                    }

                case 7:
                    {
                        newValue = faker.Company.CompanyName();
                        while (newValue == old)
                            newValue = faker.Company.CompanyName();
                        break;
                    }

                case 8:
                    {
                        newValue = faker.Name.JobTitle();
                        while (newValue == old)
                            newValue = faker.Name.JobTitle();
                        break;
                    }

                case 9:
                    {
                        newValue = faker.Finance.CreditCardNumber();
                        while (newValue == old)
                            newValue = faker.Finance.CreditCardNumber();
                        break;
                    }

                case 10:
                    {
                        newValue = faker.Address.StreetName();
                        while (newValue == old)
                            newValue = faker.Address.StreetName();
                        break;
                    }

                case 11:
                    {
                        newValue = faker.Address.SecondaryAddress();
                        while (newValue == old)
                            newValue = faker.Address.SecondaryAddress();
                        break;
                    }

                case 12:
                    {
                        newValue = faker.Address.City();
                        while (newValue == old)
                            newValue = faker.Address.City();
                        break;
                    }

                case 13:
                    {
                        newValue = faker.Address.County();
                        while (newValue == old)
                            newValue = faker.Address.County();
                        break;
                    }

                case 14:
                    {
                        newValue = faker.Address.State();
                        while (newValue == old)
                            newValue = faker.Address.State();
                        break;
                    }

                case 15:
                    {
                        newValue = faker.Address.ZipCode();
                        while (newValue == old)
                            newValue = faker.Address.ZipCode();
                        break;
                    }

                case 16:
                    {
                        newValue = faker.Address.Country();
                        while (newValue == old)
                            newValue = faker.Address.Country();
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
            SetAttribute(i, newValue);

            return newValue + $"|Changed attribute: '{GetAttributeName(i)}' from '{old}' to '{newValue}'";
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
                case 0: Title = value; break;
                case 1: FirstName = value; break;
                case 2: LastName = value; break;
                case 3: Email = value; break;
                case 4: Phone = value; break;
                case 5: Gender = value; break;
                case 6: Age = ParseNullableInt(value); break;
                case 7: Company = value; break;
                case 8: JobTitle = value; break;
                case 9: CreditCardNumber = value; break;
                case 10: Street = value; break;
                case 11: StreetNumber = value; break;
                case 12: City = value; break;
                case 13: County = value; break;
                case 14: State = value; break;
                case 15: ZipCode = value; break;
                case 16: Country = value; break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }

        internal int? ParseNullableInt(string input)
        {
            if (input == null)
                return null;

            if (!int.TryParse(input, out var result))
                throw new FormatException($"Cannot parse '{input}'.");
            
            return result;
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