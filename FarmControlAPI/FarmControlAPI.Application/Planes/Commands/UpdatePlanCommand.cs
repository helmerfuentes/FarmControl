using FarmControlAPI.Application.Common;
using FarmControlAPI.Application.Planes.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Planes.Commands;

public record UpdatePlanCommand(int Id, string Nombre, string? Descripcion, int MaxFincas, int MaxUsuarios) : IRequest<Result<PlanDto>>;

public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, Result<PlanDto>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public UpdatePlanCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<PlanDto>> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
	{
		Result<PlanDto> result;

		var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
		if (plan is null)
		{
			result = Result<PlanDto>.Failure($"Plan con Id {request.Id} no encontrado.");
			return result;
		}

		plan.Nombre = request.Nombre;
		plan.Descripcion = request.Descripcion;
		plan.MaxFincas = request.MaxFincas;
		plan.MaxUsuarios = request.MaxUsuarios;

		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync("Plan actualizado", $"Se actualizó el plan '{plan.Nombre}' (fincas: {plan.MaxFincas}, usuarios: {plan.MaxUsuarios}).", cancellationToken: cancellationToken);

		result = Result<PlanDto>.Success(new PlanDto(plan.Id, plan.Nombre, plan.Descripcion, plan.MaxFincas, plan.MaxUsuarios));
		return result;
	}
}
