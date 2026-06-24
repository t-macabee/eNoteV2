using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.MusicStores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/music-stores")]
public sealed class AdminMusicStoreController(IMusicStoreService service)
    : ReferenceCrudController<MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(service)
{
}
