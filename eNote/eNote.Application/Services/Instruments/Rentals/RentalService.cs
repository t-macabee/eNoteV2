using eNote.Application.DTOs;
using eNote.Application.Interfaces.Instruments.InstrumentRentals;
using eNote.Application.Requests.InstrumentRental;
using eNote.Application.SearchObjects;

namespace eNote.Application.Services.Instruments.Rentals
{
    public class RentalService(IRentalQueryService query, IRentalCommandService command) : IRentalService
    {
        private readonly IRentalQueryService _query = query;
        private readonly IRentalCommandService _command = command;

        public Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId) => _query.GetByIdForShopAsync(rentalId, userId);
        public Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId) => _query.GetByIdForStudentAsync(rentalId, userId);

        public Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject) => _query.GetPagedForShopAsync(userId, searchObject);
        public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject) => _query.GetPagedForStudentAsync(userId, searchObject);        

        public Task<InstrumentRentalDto> CreateRequestAsync(int studentId, RentalCreateRequest request) => _command.CreateRequestAsync(studentId, request);
        public Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response) => _command.ApproveAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response) => _command.RejectAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response) => _command.PickupAsync(rentalId, userId, response);
        public Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response) => _command.CompleteAsync(rentalId, userId, response);                          
    }
}
