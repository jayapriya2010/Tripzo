using AutoMapper;
using System.Linq;
using System.Collections.Generic;
using Tripzo.Models;
using Tripzo.DTOs.Operator;
using Tripzo.DTOs.Passenger;
using Tripzo.DTOs.Admin; // some files use this namespace
using Tripzo.DTO.Admin;  // other files use this namespace (inconsistent in repo)
using Tripzo.DTOs; // for UserRegisterDTO

// Alias to avoid ambiguity with Microsoft.AspNetCore.Routing.Route
using RouteModel = Tripzo.Models.Route;

namespace Tripzo.Mapping
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // Operator mappings
            CreateMap<Bus, BusDTO>().ReverseMap();
            CreateMap<BusCreateDTO, Bus>();

            CreateMap<SeatConfigDTO, SeatConfig>();
            CreateMap<SeatConfig, Tripzo.DTOs.Passenger.SeatLayoutDTO>()
                .ForMember(d => d.SeatId, opt => opt.MapFrom(s => s.SeatId))
                .ForMember(d => d.SeatNumber, opt => opt.MapFrom(s => s.SeatNumber))
                .ForMember(d => d.SeatType, opt => opt.MapFrom(s => s.SeatType))
                .ForMember(d => d.IsAvailable, opt => opt.Ignore())
                .ForMember(d => d.FinalPrice, opt => opt.Ignore());

            CreateMap<RouteCreateDTO, RouteModel>();
            CreateMap<StopDTO, RouteStop>();

            // Legacy mapping (BookingRequestDTO no longer used with Razorpay flow)
            CreateMap<BookingRequestDTO, Booking>()
                .ForMember(d => d.TotalAmount, opt => opt.Ignore())
                .ForMember(d => d.BookingDate, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.BookedSeats, opt => opt.Ignore())
                .ForMember(d => d.Payment, opt => opt.Ignore());

            CreateMap<Booking, BookingResponseDTO>();

            CreateMap<Booking, PassengerHistoryDTO>()
                .ForMember(d => d.RouteName, opt => opt.MapFrom(s => s.Route != null ? s.Route.SourceCity + " -> " + s.Route.DestCity : null))
                .ForMember(d => d.BusNumber, opt => opt.MapFrom(s => s.Route != null && s.Route.Bus != null ? s.Route.Bus.BusNumber : null));

            CreateMap<RouteModel, BusSearchResultDTO>()
                .ForMember(d => d.RouteId, opt => opt.MapFrom(r => r.RouteId))
                .ForMember(d => d.BusName, opt => opt.MapFrom(r => r.Bus != null ? r.Bus.BusName : null))
                .ForMember(d => d.BusType, opt => opt.MapFrom(r => r.Bus != null ? r.Bus.BusType : null))
                .ForMember(d => d.DepartureTime, opt => opt.Ignore())
                .ForMember(d => d.Fare, opt => opt.MapFrom(r => r.BaseFare))
                .ForMember(d => d.Amenities, opt => opt.MapFrom(r => r.Bus != null && r.Bus.BusAmenities != null ? r.Bus.BusAmenities.Select(ba => ba.Amenity.AmenityName).ToList() : new List<string>()))
                .ForMember(d => d.AvailableSeats, opt => opt.Ignore());

            // Admin mappings
            CreateMap<AmenityMaster, AmenityDTO>().ReverseMap();
            CreateMap<CreateAmenityDTO, AmenityMaster>();
            CreateMap<ErrorLog, ErrorLogDTO>().ReverseMap();
            CreateMap<Booking, GlobalBookingDTO>()
                .ForMember(d => d.PassengerName, opt => opt.MapFrom(b => b.User != null ? b.User.FullName : null))
                .ForMember(d => d.RouteName, opt => opt.MapFrom(b => b.Route != null ? b.Route.SourceCity + " to " + b.Route.DestCity : null));

            CreateMap<Booking, BookingDetailAdminDTO>().ReverseMap();

            CreateMap<User, UserListDTO>()
                .ForMember(d => d.UserId, opt => opt.MapFrom(u => u.UserId));

            CreateMap<Tripzo.DTOs.UserRegisterDTO, User>()
                .ForMember(d => d.PasswordHash, opt => opt.MapFrom(s => s.Password))
                .ForMember(d => d.UserId, opt => opt.Ignore());

            // Payments
            CreateMap<Payment, Tripzo.DTO.Admin.BookingDetailAdminDTO>().ReverseMap();

            // BookedSeat mappings if needed
            CreateMap<BookedSeat, Tripzo.DTOs.Operator.PassengerManifestDTO>()
                .ForMember(d => d.SeatNumber, opt => opt.MapFrom(bs => bs.Seat != null ? bs.Seat.SeatNumber : null))
                .ForMember(d => d.PassengerName, opt => opt.MapFrom(bs => bs.Booking != null && bs.Booking.User != null ? bs.Booking.User.FullName : null))
                .ForMember(d => d.BookingStatus, opt => opt.MapFrom(bs => bs.Booking != null ? bs.Booking.Status : null))
                .ForMember(d => d.ContactNumber, opt => opt.Ignore())
                .ForMember(d => d.BoardingPoint, opt => opt.Ignore())
                .ForMember(d => d.DroppingPoint, opt => opt.Ignore());
        }
    }
}
