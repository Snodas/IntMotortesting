using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using WebApplication1.Services;
using ZiggyCreatures.Caching.Fusion;

namespace WebApplication1.Controllers
{
    public class PlatinaController : Controller
    {
        private readonly IFusionCache _cache;
        private readonly IPlatinaService _platinaService;
        private readonly ILogger<PlatinaController> _logger;
        private readonly IMapper _mapper;

        public PlatinaController(
            IFusionCache cache,
            IPlatinaService platinaService,
            ILogger<PlatinaController> logger,
            IMapper mapper)
        {
            _cache = cache;
            _platinaService = platinaService;
            _logger = logger;
            _mapper = mapper;
        }
    }
}
