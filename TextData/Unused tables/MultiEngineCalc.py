from math import sqrt

def bisect_solver_zero(f, d_min, d_max, eps):
    if f(d_min) * f(d_max) > eps:
        print('f(' + str(d_min) + ') = ' + str(f(d_min)), 'f(' + str(d_max) + ') = ' + str(f(d_max)))
        return
    a = d_min
    b = d_max
    mid = 0.5 * (a + b)
    while abs(f(mid)) > eps:
        #print('f(' + str(mid) + ') = ',f(mid), 'Domain is [' + str(a) + ', ' + str(b) + ']')
        if f(mid) * f(d_min) > 0:
            a = mid
        elif f(mid) * f(d_max) > 0:
            b = mid
        mid = 0.5 * (a + b)
    return mid

def bisect_solver(f, target, d_min, d_max, eps):
    res = bisect_solver_zero(lambda x: f(x) - target, d_min, d_max, eps)
    print('f(' + str(res) + ') =',f(res))
    return res

factor = 1.2
speed_func = lambda base, num_comps: 0.1  * (base + sqrt(num_comps * factor))
acc_func =   lambda base, num_comps: 0.01 * (base + sqrt(num_comps * factor))
brk_func =   lambda base, num_comps: 0.1  * (base + sqrt(num_comps * factor))

targets = [
    ('Monitor/Richmond', 2, 0.9  , 0.1   , 0.9   ),
    ('Charleston',       2, 0.92 , 0.11  , 0.99  ),
    ('Cochrane',         3, 0.85 , 0.087 , 0.783 ),
    ('Chickasaw',        3, 0.825, 0.085 , 0.765 ),
    ('Sachsen',          4, 0.775, 0.0775, 0.6975),
    ('Roanoke',          4, 0.75 , 0.073 , 0.657 ),
    ('Kaiser Max',       5, 0.725, 0.068 , 0.612 ),
    ('Aurora',           5, 0.715, 0.066 , 0.594 ),
    ('Arminius',         5, 0.7  , 0.064 , 0.576 ),
    ('Hermes',           5, 0.67 , 0.06  , 0.54  ),
    ('Navarino',         6, 0.635, 0.057 , 0.513 ),
    ('Marceau',          6, 0.635, 0.057 , 0.513 ),
    ('Devastation',      6, 0.625, 0.053 , 0.477 )
    ]

for (ship, n_comps, target_speed, target_acc, target_brk) in targets:
    print(ship)
    base_speed = bisect_solver(lambda x: speed_func(x, n_comps), target_speed, 0.0, 10.0, 1e-4)
    base_acc = bisect_solver(lambda x: acc_func(x, n_comps), target_acc, 0.0, 10.0, 1e-4)
    base_brk = bisect_solver(lambda x: brk_func(x, n_comps), target_brk, 0.0, 10.0, 1e-4)
    print('Base speed, acceleration, braking:', base_speed, base_acc, base_brk)
    print()

