﻿using System;

namespace ObjectHashServer.Models.API.Response
{
    public abstract class ResponseModel
    {
        public ResponseModel(string obj)
        {
            if (string.IsNullOrWhiteSpace(obj))
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Object = obj;
        }

        public string Object { get; private set; }
    }
}
