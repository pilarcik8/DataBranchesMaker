using Bogus;
using System.Xml.Serialization;

namespace Shared
{
    // Cely Person.cs zalezi na tom aby sa nemenili tieto atributy a ich indexy
    // Kazdy druhy atribut je aby sa pocas runntimu dal "pridat" novy atribut (nastaveny na null == neexistuje pre xml)
    public class Person
    {
        [XmlElement(Order = 1)]
        public string? Iban;
        [XmlElement(Order = 2)]
        public string? Title { get; set; } //0
        /*---------------------------------------*/
        [XmlElement(Order = 3)]
        public string? FavouriteColor;
        [XmlElement(Order = 4)]
        public string? FirstName { get; set; } //1
        /*---------------------------------------*/
        [XmlElement(Order = 5)]
        public string? BitcoinAddress;
        [XmlElement(Order = 6)]
        public string? LastName { get; set; } //2
        /*---------------------------------------*/
        [XmlElement(Order = 7)]
        public string? EmailUserName;
        [XmlElement(Order = 8)]
        public string? Email { get; set; } //3
        /*---------------------------------------*/
        [XmlElement(Order = 9)]
        public string? PhoneExtension;
        [XmlElement(Order = 10)]
        public string? Phone { get; set; } //4
        /*---------------------------------------*/
        [XmlElement(Order = 11)]
        public string? FavouriteWord;
        [XmlElement(Order = 12)]
        public string? Gender { get; set; }  //5
        /*---------------------------------------*/
        [XmlElement(Order = 13)]
        public string? FavouriteMusicGenre;
        [XmlElement(Order = 14)]
        public string? StreetNumber { get; set; } //6

        /*---------------------------------------*/
        [XmlElement(Order = 15)]
        public string? CompanyCatchPhrase;
        [XmlElement(Order = 16)]
        public string? Company { get; set; } //7
        /*---------------------------------------*/
        [XmlElement(Order = 17)]
        public string? JobDescriptor;
        [XmlElement(Order = 18)]
        public string? JobTitle { get; set; } //8
        /*---------------------------------------*/
        [XmlElement(Order = 19)]
        public string? CreditAccount;
        [XmlElement(Order = 20)]
        public string? CreditCardNumber { get; set; } //9
        /*---------------------------------------*/
        [XmlElement(Order = 21)]
        public string? StreetSuffix;
        [XmlElement(Order = 22)]
        public string? Street { get; set; } //10
        /*---------------------------------------*/
        [XmlElement(Order = 23)]
        public string? CityPrefix;
        [XmlElement(Order = 24)]
        public string? City { get; set; } //11
        /*---------------------------------------*/
        [XmlElement(Order = 25)]
        public string? CountyCode;
        [XmlElement(Order = 26)]
        public string? County { get; set; } //12
        /*---------------------------------------*/
        [XmlElement(Order = 27)]
        public string? StateAbbr;
        [XmlElement(Order = 28)]
        public string? State { get; set; } //13
        /*---------------------------------------*/
        [XmlElement(Order = 29)]
        public string? ZipPlus4;
        [XmlElement(Order = 30)]
        public string? ZipCode { get; set; } //14
        /*---------------------------------------*/
        [XmlElement(Order = 31)]
        public string? CountryCode;
        [XmlElement(Order = 32)]
        public string? Country { get; set; } //15

        public Person()
        {
            Title = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Gender = string.Empty;
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
            CityPrefix = null;
            CountyCode = null;
            StateAbbr = null;
            ZipPlus4 = null;
        }

        public string[] AddAttribute(int i, Faker faker, string preGeneratedValue = "")
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
                    value = hasPreGen ? preGeneratedValue : faker.Address.CityPrefix();
                    this.CityPrefix = value;
                    name = nameof(CityPrefix);
                    break;

                case 12:
                    value = hasPreGen ? preGeneratedValue : faker.Random.AlphaNumeric(5);
                    this.CountyCode = value;
                    name = nameof(CountyCode);
                    break;

                case 13:
                    value = hasPreGen ? preGeneratedValue : faker.Address.StateAbbr();
                    this.StateAbbr = value;
                    name = nameof(StateAbbr);
                    break;

                case 14:
                    value = hasPreGen ? preGeneratedValue : faker.Random.Number(1000, 9999).ToString();
                    this.ZipPlus4 = value;
                    name = nameof(ZipPlus4);
                    break;
                case 15:
                    value = hasPreGen ? preGeneratedValue : faker.Address.CountryCode();
                    this.CountryCode = value;
                    name = nameof(CountryCode);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }

            string[] valueAndName = new[] { value, name };
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
                        newValue = faker.Address.BuildingNumber();
                        while (newValue == old)
                            newValue = faker.Address.BuildingNumber();
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
                        newValue = faker.Address.City();
                        while (newValue == old)
                            newValue = faker.Address.City();
                        break;
                    }

                case 12:
                    {
                        newValue = faker.Address.County();
                        while (newValue == old)
                            newValue = faker.Address.County();
                        break;
                    }

                case 13:
                    {
                        newValue = faker.Address.State();
                        while (newValue == old)
                            newValue = faker.Address.State();
                        break;
                    }

                case 14:
                    {
                        newValue = faker.Address.ZipCode();
                        while (newValue == old)
                            newValue = faker.Address.ZipCode();
                        break;
                    }

                case 15:
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

            return newValue;
        }

        public string? GetAttribute(int i)
        {
            return i switch
            {
                0 => Title,
                1 => FirstName,
                2 => LastName,
                3 => Email,
                4 => Phone,
                5 => Gender,
                6 => StreetNumber,
                7 => Company,
                8 => JobTitle,
                9 => CreditCardNumber,
                10 => Street,
                11 => City,
                12 => County,
                13 => State,
                14 => ZipCode,
                15 => Country,
                _ => throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index"),
            };
        }

        public void RemoveAtribute(int i)
        {
            SetAttribute(i, null);
        }


        public void SetAttribute(int i, string? value)
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
                case 6: StreetNumber = value; break;
                case 7: Company = value; break;
                case 8: JobTitle = value; break;
                case 9: CreditCardNumber = value; break;
                case 10: Street = value; break;
                case 11: City = value; break;
                case 12: County = value; break;
                case 13: State = value; break;
                case 14: ZipCode = value; break;
                case 15: Country = value; break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index");
            }
        }

        public string GetAttributeName(int i)
        {
            return i switch
            {
                0 => nameof(Title),
                1 => nameof(FirstName),
                2 => nameof(LastName),
                3 => nameof(Email),
                4 => nameof(Phone),
                5 => nameof(Gender),
                6 => nameof(StreetNumber),
                7 => nameof(Company),
                8 => nameof(JobTitle),
                9 => nameof(CreditCardNumber),
                10 => nameof(Street),
                11 => nameof(City),
                12 => nameof(County),
                13 => nameof(State),
                14 => nameof(ZipCode),
                15 => nameof(Country),
                _ => throw new ArgumentOutOfRangeException(nameof(i), i, "Invalid attribute index"),
            };
        }

        // iba zakladné atribúty
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
                StreetNumber = this.StreetNumber,
                Company = this.Company,
                JobTitle = this.JobTitle,
                CreditCardNumber = this.CreditCardNumber,
                Street = this.Street,
                City = this.City,
                County = this.County,
                State = this.State,
                ZipCode = this.ZipCode,
                Country = this.Country
            };
        }
    }
}