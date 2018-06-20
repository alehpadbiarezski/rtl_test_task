using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rtl.Api.Model;
using TvMazeScrapper.Core.Abstract;
using TvMazeScrapper.Core.Model;

namespace Rtl.Api.Controllers
{
    [Route("api/[controller]")]
    public class ShowsController : Controller
    {
        private readonly IStorage _storage;
        private readonly IConfiguration _config;

        public ShowsController(IStorage storage, IConfiguration config)
        {
            _storage = storage;
            _config = config;
        }

        private int PageSize => int.Parse(_config["PageSize"]);

        [HttpGet]
        public IActionResult Get([FromQuery]int page = 0)
        {
            var result = _storage.GetShows(page, PageSize);
            if (0 == result.Count)
            {
                return NotFound();
            }
            return Ok(result
                .Select(show =>
                    {
                        show.Cast = show.Cast.OrderByDescending(a => a.Birthday).ToArray();
                        return new OutputShow()
                        {
                            RawId = show.RawId,
                            Name = show.Name,
                            Cast = show.Cast
                                .OrderByDescending(a => a.Birthday)
                                .Select(actor => new OutputActor()
                                {
                                    RawId = actor.RawId,
                                    Name = actor.Name,
                                    Birthday = actor.Birthday
                                })
                                .ToArray()
                        };
                    })
                .ToList());
        }
    }
}
