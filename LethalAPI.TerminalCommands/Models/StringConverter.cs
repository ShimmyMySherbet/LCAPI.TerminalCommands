﻿// -----------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="LethalAPI Modding Community">
// Copyright (c) LethalAPI Modding Community. All rights reserved.
// Licensed under the LGPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace LethalAPI.TerminalCommands.Models;

using System;
using System.Collections.Concurrent;
using System.Reflection;

using Attributes;

/// <summary>
/// Delegate that converts a string to a given object, or thows <see cref="ArgumentException"/> if the input cannot be converted.
/// </summary>
/// <param name="input">String input to convert.</param>
/// <returns>The parsed value.</returns>
/// <exception cref="ArgumentException">Thrown if the input cannot be parsed.</exception>
public delegate object StringConversionHandler(string input);

/// <summary>
/// Provides services for parsing user-entered strings into types, including custom game types.
/// </summary>
public static class StringConverter
{
    /// <summary>
    /// Specifies if the default string converters have been registered yet.
    /// </summary>
    private static bool initialized;

    /// <summary>
    /// Gets the registry of string converters.
    /// </summary>
    /// <remarks>
    /// Register new converters using <see cref="RegisterFrom{T}(T, bool)"/>.
    /// </remarks>
    public static ConcurrentDictionary<Type, StringConversionHandler> StringConverters { get; } = new ();

    /// <summary>
    /// Attempts to convert the specified string to the specified type.
    /// </summary>
    /// <param name="value">String value to parse.</param>
    /// <param name="type">The type to parse the string as.</param>
    /// <param name="result">Resulting object instance, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the string could be parsed as the specified type.</returns>
    public static bool TryConvert(string value, Type type, out object result)
    {
        if (!initialized)
        {
            initialized = true;
            RegisterFromType(typeof(DefaultStringConverters), replaceExisting: false);
        }

        if (!StringConverters.TryGetValue(type, out StringConversionHandler? converter))
        {
            result = null;
            return false;
        }

        try
        {
            result = converter(value);
            return true;
        }
        catch (Exception)
        {
            // Failed to parse as type, return null
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Registers all string converters from a class instance.
    /// </summary>
    /// <remarks>
    /// String converters return any type, have only a string as a parameter, and are decorated with <see cref="StringConverterAttribute"/>.
    /// </remarks>
    /// <typeparam name="T">Type to register from.</typeparam>
    /// <param name="instance">Class instance.</param>
    /// <param name="replaceExisting">When <see langword="true"/>, existing converters for types will be replaced.</param>
    public static void RegisterFrom<T>(T instance, bool replaceExisting = true)
        where T : class
    {
        RegisterFromType(typeof(T), instance, replaceExisting);
    }

    /// <summary>
    /// Registers all string converters from a class instance or static class.
    /// </summary>
    /// <remarks>
    /// String converters return any type, have only a string as a parameter, and are decorated with <see cref="StringConverterAttribute"/>.
    /// </remarks>
    /// <param name="type">The class type to register from.</param>
    /// <param name="instance">Class instance, or null if the class is static.</param>
    /// <param name="replaceExisting">When <see langword="true"/>, existing converters for types will be replaced.</param>
    public static void RegisterFromType(Type type, object instance = null, bool replaceExisting = true)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.GetCustomAttribute<StringConverterAttribute>() == null)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length != 1)
            {
                continue;
            }

            if (parameters[0].ParameterType != typeof(string))
            {
                continue;
            }

            Type resultingType = method.ReturnType;

            StringConversionHandler converter = (value) => method.Invoke(instance, new object[] { value });

            if (replaceExisting || !StringConverters.ContainsKey(resultingType))
            {
                StringConverters[resultingType] = converter;
            }
        }
    }
}