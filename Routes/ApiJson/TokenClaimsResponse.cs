﻿using Gaos.Auth;

namespace Gaos.Routes.ApiJson
{
    public class TokenClaimsResponse
    {
        public bool? isError { get; set; }

        public string? errorMessage { get; set; }

        public TokenClaims? tokenClaims { get; set; }


    }
}
