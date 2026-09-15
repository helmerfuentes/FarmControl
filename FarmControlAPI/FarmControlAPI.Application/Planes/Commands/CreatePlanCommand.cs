using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Planes.Queries;
using FarmControlAPI.Domain.Entities;
using MediatR;

namespace FarmControlAPI.Application.Planes.Commands;

public record CreatePlanCommand(string Nombre, string? Descripcion, int MaxFincas, int MaxUsuarios) : IRequest<Result<PlanDto>>;

public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Result<PlanDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public CreatePlanCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<PlanDto>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
	{
		Result<PlanDto> result;

		var plan = new Plan
		{
			Nombre = request.Nombre,
			Descripcion = request.Descripcion,
			MaxFincas = request.MaxFincas,
			MaxUsuarios = request.MaxUsuarios
		};

		_context.Planes.Add(plan);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync("Plan creado", $"Se creó el plan '{plan.Nombre}' (fincas: {plan.MaxFincas}, usuarios: {plan.MaxUsuarios}).", cancellationToken: cancellationToken);

		result = Result<PlanDto>.Success(new PlanDto(plan.Id, plan.Nombre, plan.Descripcion, plan.MaxFincas, plan.MaxUsuarios));
		return result;
	}
}
