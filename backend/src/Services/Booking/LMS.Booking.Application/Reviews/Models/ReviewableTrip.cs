using LMS.Booking.Domain.Reviews;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Reviews.Models;

// 本人已完成的行程及其评价（没有评价为 null）。已完成即可评价、可修改；评价窗口与修改期限待业务确认（Q2-06），目前不设期限。
public sealed record ReviewableTrip(Trip Trip, TripReview? Review);
