using DomainLayer.Managers.ConfigurationProviders;
using DomainLayer.Managers.DataLayer;
using DomainLayer.Managers.Enums;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.Parsers;
using DomainLayer.Managers.ServiceLocators;
using DomainLayer.Managers.Services.ImdbService;
using DomainLayer.Managers.Validators;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DomainLayer.Managers
{
    internal sealed class MovieManager : IDisposable
    {
        private bool _disposed;
        private readonly ServiceLocatorBase _serviceLocator;
        private ConfigurationProviderBase _configurationProvider;
        private ConfigurationProviderBase ConfigurationProvider { get { return _configurationProvider ?? (_configurationProvider = _serviceLocator.CreateConfigurationProvider()); } }

        private ImdbServiceGateway _imdbServiceGateway;
        private ImdbServiceGateway ImdbServiceGateway
        {
            get
            {
                return _imdbServiceGateway ?? (_imdbServiceGateway = _serviceLocator.CreateImdbServiceGateway(ConfigurationProvider.GetImdbServiceBaseUrl()));
            }
        }

        private DataFacade _dataFacade;
        private DataFacade DataFacade { get { return _dataFacade ?? (_dataFacade = new DataFacade()); } }

        public MovieManager(ServiceLocatorBase serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public async Task CreateMovie(Movie movie)
        {
            ValidatorMovie.EnsureMovieIsValid(movie);
            await DataFacade.CreateMovie(movie).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Movie>> GetAllMovies()
        {
            var moviesTask = ImdbServiceGateway.GetAllMovies();
            var moviesFromDbTask = DataFacade.GetAllMovies();

            await Task.WhenAll(moviesTask, moviesFromDbTask).ConfigureAwait(false);

            var movies = moviesTask.Result;
            var moviesFromDb = moviesFromDbTask.Result;

            var moviesList = movies.ToList();
            moviesList.AddRange(moviesFromDb);
            return moviesList;
        }

        public async Task<IEnumerable<Movie>> GetMoviesByGenre(Genre genre)
        {
            GenreParser.Validate(genre);
            var allMovies = await GetAllMovies().ConfigureAwait(false);
            return allMovies.Where(m => m.Genre == genre);
        }

        [ExcludeFromCodeCoverage]
        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _imdbServiceGateway?.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
