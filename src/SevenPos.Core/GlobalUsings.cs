// The business areas of Core reference each other (a Ticket has a Guest and Garments,
// a Shift has Payments), so their namespaces are imported once for the whole project.
global using SevenPos.Core.Cash;
global using SevenPos.Core.Catalog;
global using SevenPos.Core.Common;
global using SevenPos.Core.Guests;
global using SevenPos.Core.Organization;
global using SevenPos.Core.Tickets;
