using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PokemonReviewApp.Dto;
using PokemonReviewApp.Interfaces;
using PokemonReviewApp.Models;
using PokemonReviewApp.Repository;

namespace PokemonReviewApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PokemonController : Controller
    {
        private readonly IPokemonRepository _pokemonRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public PokemonController(IPokemonRepository pokemonRepository,IReviewRepository reviewRepository,IReviewerRepository reviewerRepository,ICategoryRepository categoryRepository, IMapper mapper)
        {
            _pokemonRepository = pokemonRepository;
            _reviewRepository = reviewRepository;
            _reviewerRepository = reviewerRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Pokemon>))]
        public IActionResult GetPokemons()
        {
            var pokemons = _mapper.Map<List<PokemonDto>>(_pokemonRepository.GetPokemons());

            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            return Ok(pokemons);
        }

        [HttpGet("{PokeId}")]
        [ProducesResponseType(200, Type = typeof(Pokemon))]
        [ProducesResponseType(400)]
        public IActionResult GetPokemon(int PokeId)
        {
            if (!_pokemonRepository.PokemonExists(PokeId))
                return NotFound();
            
            var pokemon = _mapper.Map<PokemonDto>(_pokemonRepository.GetPokemon(PokeId));

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(pokemon);

            
        }

        [HttpGet("{PokeId}/rating")]
        [ProducesResponseType(200, Type = typeof(decimal))]
        [ProducesResponseType(400)]
        public IActionResult GetPokemonRating(int PokeId)
        {
            if (!_pokemonRepository.PokemonExists(PokeId))
                return NotFound();
            
            var rating = _pokemonRepository.GetPokemonRating(PokeId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(rating);  
        }

        [HttpPost]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public IActionResult CreateOwner([FromQuery] int ownerId, [FromQuery] int categoryId, [FromBody] PokemonDto pokemonCreate)
        {
            if (pokemonCreate == null)
                return BadRequest(ModelState);

            var pokemon = _pokemonRepository.GetPokemons().Where(c => c.Name.Trim().ToUpper() == pokemonCreate.Name.Trim().ToUpper()).FirstOrDefault();

            if (pokemon != null)
            {
                ModelState.TryAddModelError("", "Pokemon already exists");
                return StatusCode(422, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pokemonMap = _mapper.Map<Pokemon>(pokemonCreate);

            if (!_pokemonRepository.CreatePokemon(ownerId,categoryId,pokemonMap))
            {
                ModelState.TryAddModelError("", "Something wenrt wrong while saving");
                return StatusCode(500, ModelState);
            }

            return Ok("Successfully created");


        }

        [HttpPut("{PokemonId}")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult UpdatePokemon([FromQuery] int ownerId, [FromQuery] int categoryId, int pokemonId, [FromBody] PokemonDto updatedPokemon)
        {
            if (updatedPokemon == null)
                return BadRequest(ModelState);

            if (pokemonId != updatedPokemon.Id)
                return BadRequest(ModelState);

            if (!_pokemonRepository.PokemonExists(pokemonId))
                return NotFound();

            if (!ModelState.IsValid)
                return BadRequest();

            var pokemonMap = _mapper.Map<Pokemon>(updatedPokemon);

            if (!_pokemonRepository.UpdatePokemon(ownerId,categoryId,pokemonMap))
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);

            }

            return NoContent();


        }

        [HttpDelete("{pokemonId}")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeletePokemon(int pokemonId)
        {
            if (_pokemonRepository.PokemonExists(pokemonId))
                return BadRequest(ModelState);
            var reviewsToDelete = _reviewRepository.GetReviewsOfAPokemon(pokemonId);

            var pokemonToDelete = _pokemonRepository.GetPokemon(pokemonId);

            if (!_reviewRepository.DeleteReviews(reviewsToDelete.ToList()))
            {
                ModelState.AddModelError("", "Something went wrong");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_pokemonRepository.DeletePokemon(pokemonToDelete))
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }
            return NoContent();
        }

    }
}
