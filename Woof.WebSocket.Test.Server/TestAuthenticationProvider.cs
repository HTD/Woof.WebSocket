using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woof.WebSocket.Test.Server {

    public class TestAuthenticationProvider : IAuthenticationProvider {

        const string TestApiKey = "x5AvVKfex7b+xOPTAsKGnPqmCNj3HCPiCBUGDyg4ZJn6DHeVn8eGzGBeqLAtxKwRugsa9UEp4IMfYbCNKRrzcA==";
        const string TestApiSecret = "8fRjPaT1YsN6kwGMxeZ9SZW1Za8gcN5cQFgfG+Ooie8e3QUMpZlVrN5h/6QNvATykHaADA6gSQ5qLDDd33xAlw==";

        public async Task<byte[]> GetKeyAsync(byte[] apiKey) {
            var testApiKey = Convert.FromBase64String(TestApiKey);
            await Task.Delay(1);
            if (apiKey.SequenceEqual(testApiKey)) return Convert.FromBase64String(TestApiSecret);
            return null;
        }

    }

}