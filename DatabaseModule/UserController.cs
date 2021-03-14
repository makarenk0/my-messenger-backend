using MyMessengerBackend.DeserializedPayloads;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    public class UserController
    {
        private readonly IMongoRepository<User> _usersRepository;

        public UserController(IMongoRepository<User> usersRepository)
        {
            _usersRepository = usersRepository;
        }

        public StatusResponse Register(RegistrationPayload payload)
        {
            User checkLogin = _usersRepository.FindOneAsync(x => x.Login == payload.Login).Result;
            if(checkLogin != null)
            {
                return new StatusResponse("error", "Login is already taken");
            }
            byte[] passwordSalt = CreateSalt(32); //best practice - salt length same as the length of hash function
            byte[] hashSaltedPassword = GenerateSaltedHash(Encoding.ASCII.GetBytes(payload.Password), passwordSalt);

            string saltBase64Representation = Convert.ToBase64String(passwordSalt);
            string base64PasswordRepresentation = Convert.ToBase64String(hashSaltedPassword);

            payload.BirthDate = payload.BirthDate.Substring(0, 15);
            string dateFormat = "ddd MMM dd yyyy";
            DateTime date = DateTime.ParseExact(payload.BirthDate, dateFormat, CultureInfo.InvariantCulture);

            User newUser = new User() { Login = payload.Login, FirstName = payload.FirstName, 
                LastName = payload.LastName, BirthDate = date, 
                PasswordHash = base64PasswordRepresentation, PasswordSalt = saltBase64Representation
            };
            _usersRepository.InsertOneAsync(newUser);
    
            return new StatusResponse("success", "Account successfully created!");
        }


        public LoginResponsePayload Login(LoginPayload payload)
        {
            User user = _usersRepository.FindOneAsync(x => x.Login == payload.Login).Result;
            if(user == null)
            {
                return new LoginResponsePayload("error", "Login does not exist!");
            }
            if(!VerifyUserPassword(user, payload.Password))
            {
                return new LoginResponsePayload("error", "Password is invalid!");
            }
            return new LoginResponsePayload("success", "logged in successfully", Guid.NewGuid().ToString());
        }

        private bool VerifyUserPassword(User user, string inputPassword)
        {
            String base64Hashed = Convert.ToBase64String(GenerateSaltedHash(Encoding.ASCII.GetBytes(inputPassword), Convert.FromBase64String(user.PasswordSalt)));
            return base64Hashed == user.PasswordHash;
        }


        private byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA256Managed();

            byte[] plainTextWithSaltBytes =
              new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }
            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }

        private byte[] CreateSalt(int size)
        {     
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }
    }
}
