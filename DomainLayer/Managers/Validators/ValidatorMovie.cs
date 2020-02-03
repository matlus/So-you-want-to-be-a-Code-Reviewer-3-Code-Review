using DomainLayer.Managers.Enums;
using DomainLayer.Managers.Exceptions;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.Parsers;
using System;

namespace DomainLayer.Managers.Validators
{
    internal static class ValidatorMovie
    {
        internal enum StringState { Null, Empty, Valid, WhiteSpaces }

        public static void EnsureMovieIsValid(Movie movie)
        {
            if (movie == null)
            {
                throw new InvalidMovieException("The movie parameter can not be null.");
            }

            var genreErrorMessage = ValidateGenre(movie.Genre);
            var titleErrorMessage = ValidateProperty("Title", movie.Title);
            var imageUrlErrorMessage = ValidateProperty("ImageUrl", movie.ImageUrl);
            var yearErrorMessage = ValidateYear(movie.Year);

            if (genreErrorMessage != null || titleErrorMessage != null || imageUrlErrorMessage != null || yearErrorMessage != null)
            {
                throw new InvalidMovieException($"{genreErrorMessage} \r\n {titleErrorMessage} \r\n {imageUrlErrorMessage} \r\n {yearErrorMessage}");
            }
        }

        private static string ValidateYear(int year)
        {
            const int minimumYear = 1900;

            if (year >= minimumYear && year <= DateTime.Today.Year)
            {
                return null;
            }

            return $"The Year, must be between {minimumYear} and {DateTime.Today.Year} (inclusive)";
        }

        private static string ValidateGenre(Genre genre)
        {
            return GenreParser.ValidationMessage(genre);
        }

        private static string ValidateProperty(string propertyName, string propertyValue)
        {
            switch (DetermineNullEmptyOrWhiteSpaces(propertyValue))
            {
                case StringState.Null:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be null";
                case StringState.Empty:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be Empty";
                case StringState.WhiteSpaces:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be Whitespaces";
                case StringState.Valid:
                default:
                    return null;
            }
        }

        private static StringState DetermineNullEmptyOrWhiteSpaces(string data)
        {
            if (data == null)
            {
                return StringState.Null;
            }
            else if (data.Length == 0)
            {
                return StringState.Empty;
            }

            foreach (var chr in data)
            {
                if (!char.IsWhiteSpace(chr))
                {
                    return StringState.Valid;
                }
            }

            return StringState.WhiteSpaces;
        }
    }
}
