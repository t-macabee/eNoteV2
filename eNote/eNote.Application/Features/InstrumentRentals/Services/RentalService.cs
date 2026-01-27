using eNote.Application.Common.Paging;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalService(IRentalQueryService query, IRentalCommandService command) : IRentalService
    {
        private readonly IRentalQueryService _query = query;
        private readonly IRentalCommandService _command = command;

        public Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId) => _query.GetByIdForShopAsync(rentalId, userId);
        public Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId) => _query.GetByIdForStudentAsync(rentalId, userId);

        public Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject) => _query.GetPagedForShopAsync(userId, searchObject);
        public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject) => _query.GetPagedForStudentAsync(userId, searchObject);        

        public Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request) => _command.CreateRequestAsync(userId, request);
        public Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response) => _command.ApproveAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response) => _command.RejectAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response) => _command.PickupAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response) => _command.CompleteAsync(rentalId, userId, response);                          
        public Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response) => _command.CancelAsync(rentalId, userId, response);                  
        public Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response) => _command.ReturnEarlyAsync(rentalId, userId, response);
    }
}
