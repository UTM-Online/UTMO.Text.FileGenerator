﻿// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="DictionaryExtensions.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Extensions
{
    using Abstract.Contracts;
    using Abstract.Exceptions;

    /// <summary>
    ///     Class DictionaryExtensions.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Merges the specified other.
        /// </summary>
        /// <param name="dict">The destination dictionary.</param>
        /// <param name="other">The source dictionary.</param>
        /// <returns>The merged context</returns>
        /// <exception cref="LocalContextKeyExistsException">
        ///     The supplied context contains a
        ///     key that already exists in the target context.
        /// </exception>
        public static Dictionary<string, object> Merge(this Dictionary<string, object> dict, Dictionary<string, object> other)
        {
            foreach (var item in other)
            {
                if (dict.ContainsKey(item.Key))
                {
                    throw new LocalContextKeyExistsException(item.Key);
                }

                dict.Add(item.Key, item.Value);
            }

            return dict;
        }
        
        public static Dictionary<string,T> AddOrUpdate<T>(this Dictionary<string,T> dictionary, string key, T value)
        {
            if (!dictionary.TryAdd(key, value))
            {
                dictionary[key] = value;
            }

            return dictionary;
        }
        
        public static async Task<List<Dictionary<string,object>>> ToTemplateContext<T>(this Dictionary<string,T> dictionary) where T : ITemplateModel
        {
            var properties = new List<Dictionary<string,object>>();
            
            foreach (var prop in dictionary.Values)
            {
                properties.Add(await prop.ToTemplateContext());
            }

            return properties;
        }
        
        public static Dictionary<string, T> JoinDictionary<T>(this Dictionary<string,T> dictionary, Dictionary<string,T> otherDictionary)
        {
            foreach (var (key, value) in otherDictionary)
            {
                dictionary.AddOrUpdate(key, value);
            }

            return dictionary;
        }
    }
}