import os, sys
import pygame, pygame.freetype, pygame.time
import argparse
import math
import csv
import json

parser = argparse.ArgumentParser(description='2D Graph visualization tool')
parser.add_argument("--profile", default=None, help="Path to a JSON profile to use")
parser.add_argument('--width', default=1280, help="The width of the visualization window", type=int)
parser.add_argument('--height', default=720, help="The height of the visualization window", type=int)
parser.add_argument("--data", default="../CSV_DATA/InfectionSummary.csv", help="The CSV file to read data from")
parser.add_argument("--title", default="Evolution graph", help="The title of the graph")
parser.add_argument("--xscale", default=5, help="Scale of the X axis", type=float)
parser.add_argument("--yscale", default=5, help="Scale of the Y axis", type=float)
parser.add_argument("--x-multiplier", default=1, help="How much to multiply the values of the X axis with", type=float)
parser.add_argument("--fullscreen", action="store_true", help="Should the window open in fullscreen mode")
parser.add_argument("--stack", action="store_true", help="Stack the curves", default=True)
parser.add_argument("--hide", nargs="+", help="Columns to hide")
parser.add_argument("--show-negative", default=False, action="store_true", help="Show negative axis (WIP)") # WORK IN PROGRESS
args = parser.parse_args()

profile = None

if args.profile is not None:
    with open(args.profile, 'r') as json_file:
        profile = json.load(json_file)

if profile and "arg_overrides" in profile:
    for override in profile["arg_overrides"]:
        setattr(args, override, profile["arg_overrides"][override])

if args.fullscreen:
    os.environ['SDL_VIDEO_WINDOW_POS'] = "%d,%d" % (0,0)

    pygame.init()
    display_size = pygame.display.Info()
    size = width, height = display_size.current_w, display_size.current_h
    screen = pygame.display.set_mode(size, pygame.NOFRAME)
else:
    os.environ['SDL_VIDEO_WINDOW_POS'] = "%d,%d" % (100, 100)

    pygame.init()
    size = width, height = args.width, args.height
    screen = pygame.display.set_mode(size)


axis_font = pygame.freetype.Font("Comfortaa-Regular.ttf", 15)

padding = 100
background_col = (2,0,13)
axis_col = (180,180,180)
curve_colors = [(70, 160, 224), (224, 70, 119), (222, 73, 217), (143, 73, 222), (73, 222, 195), (232, 146, 26), (141, 227, 30), (73, 30, 227)]
next_curve_color_index = 0

x_scale = args.xscale # The distance between two steps on the X axis
y_scale = args.yscale # The distance between two steps on the Y axis
step_size = 50 # The amount of pixels between two steps

t=1 # For animation

if args.show_negative:
    screen_graph_origin = (padding, height/2)
else:
    screen_graph_origin = (padding, height-padding)

pygame.display.set_caption("NamePointer's graph visualization tool")

class Curve:
    def __init__(self, name, color):
        self.x_coords = []
        self.y_coords = []
        self.name = name
        self.color = color
    
    def add_point(self, x, y):
        self.x_coords.append(x)
        self.y_coords.append(y)

class PointSequence:
    def __init__(self, name, color, height):
        self.x_coords = []
        self.values = []
        self.name = name
        self.color = color
        self.height = height

    def add_point(self, x, val):
        self.x_coords.append(x)
        self.values.append(val)

    def last_point(self):
        last_index = 0
        last_x = 0

        for i in range(len(self.x_coords)):
            if self.x_coords[i] > last_x:
                last_x = self.x_coords[i]
                last_index = i

        return (last_x, self.values[last_index])

def lerp(a, b, t):
    return a+(b-a)*t

def lerp2d(a, b, t):
    return (a[0]+(b[0]-a[0])*t, a[1]+(b[1]-a[1])*t)

def inverse_lerp(a, b, l):
    return (l - a) / (b - a)

def new_curve_color():
    global next_curve_color_index
    col = curve_colors[next_curve_color_index]
    next_curve_color_index+=1
    return col

def draw_axes(t=1):
    x_max_point = lerp2d(screen_graph_origin, (width-padding, screen_graph_origin[1]), t)
    y_max_point = lerp2d((padding,height-padding), (padding,padding), t)

    # Axes themselves
    pygame.draw.line(screen, axis_col, screen_graph_origin, x_max_point)
    pygame.draw.line(screen, axis_col, (padding,height-padding), y_max_point)

    # Arrows
    pygame.draw.line(screen, axis_col, y_max_point, (y_max_point[0]-5,y_max_point[1]+5))
    pygame.draw.line(screen, axis_col, y_max_point, (y_max_point[0]+5,y_max_point[1]+5))
    if args.show_negative:
        pygame.draw.line(screen, axis_col, (padding,height-padding), (padding-5,height-padding-5))
        pygame.draw.line(screen, axis_col, (padding,height-padding), (padding+5,height-padding-5))

    pygame.draw.line(screen, axis_col, x_max_point, (x_max_point[0]-5,x_max_point[1]+5))
    pygame.draw.line(screen, axis_col, x_max_point, (x_max_point[0]-5,x_max_point[1]-5))
    
    # Steps
    i = 1
    for x in range(padding+step_size, width-padding, step_size):
        if x > x_max_point[0]:
            continue
        
        opacity = 1
        distance_from_end = x_max_point[0] - x
        if t < 1 and distance_from_end < step_size:
            opacity = distance_from_end / step_size

        pygame.draw.line(screen, axis_col, (x, screen_graph_origin[1]-5*opacity), (x, screen_graph_origin[1]+5*opacity))

        text_surface, rect = axis_font.render(str(i*x_scale), (200, 200, 200, opacity * 255))
        screen.blit(text_surface,(x-rect.width/2,screen_graph_origin[1]+10))
        i+=1

    i = 1
    for y in range(height-padding-step_size, padding, -step_size):
        if y < y_max_point[1]:
            continue

        opacity = 1
        distance_from_end = y - y_max_point[1]
        if t < 1 and distance_from_end < step_size:
            opacity = distance_from_end / step_size

        pygame.draw.line(screen, axis_col, (padding-5*opacity, y), (padding+5*opacity, y))

        text_surface, rect = axis_font.render(str(i*y_scale), (200, 200, 200, opacity * 255))
        screen.blit(text_surface,(padding-10-rect.width,y-rect.height/2))
        i+=1

def draw_curve(curve, t=1):
    if len(curve.x_coords) != len(curve.y_coords):
        raise Exception("x_coords and y_coords must have same length")

    max_graph_x = (width-2*padding)/step_size*x_scale
    max_x = screen_graph_origin[0]+lerp(0, min(max_graph_x, max(curve.x_coords)), t)/x_scale*step_size

    if t <= 0 or max_x < 2:
        return # Do not bother drawing anything

    last_point = None
    for i in range(len(curve.x_coords)):
        point = (screen_graph_origin[0]+curve.x_coords[i]/x_scale*step_size, screen_graph_origin[1]-curve.y_coords[i]/y_scale*step_size)

        if last_point is not None:
            if point[0] > max_x:
                t = inverse_lerp(last_point[0], point[0], max_x)
                point = (max_x, lerp(last_point[1], point[1], t))
            pygame.draw.line(screen, curve.color, last_point, point, 2)

        last_point = point
    
    text_surface, rect = axis_font.render(curve.name, curve.color)
    screen.blit(text_surface, (last_point[0]+5,last_point[1]-rect.height/2))

def draw_point_sequence(pt_sequence):
    if len(pt_sequence.x_coords) != len(pt_sequence.values):
        raise Exception("x_coords and values must have same length")

    for i in range(len(pt_sequence.x_coords)):
        point = (math.floor(screen_graph_origin[0]+pt_sequence.x_coords[i]/x_scale*step_size), math.floor(screen_graph_origin[1]-pt_sequence.height/y_scale*step_size))
        pygame.draw.circle(screen, pt_sequence.color, point, 5)
        text_surface, rect = axis_font.render(str(pt_sequence.values[i]), pt_sequence.color)
        screen.blit(text_surface,(point[0]-rect.width/2,point[1] + 20))

# Load data
curves = []
point_sequences = []
with open(args.data, newline='') as csvfile:
    reader = csv.reader(csvfile, delimiter=";", quotechar="|")
    row_index = 0
    point_sequence_columns = []
    for row in reader:
        if row_index == 0: # First row defines names
            for i in range(len(row)):
                curve_name = row[i]
                if curve_name.startswith('#'): # If name starts with '#' it's a point sequence
                    point_sequences.append(PointSequence(curve_name[1:], new_curve_color(), 5))
                    point_sequence_columns.append(i)
                else:
                    curves.append(Curve(curve_name, new_curve_color()))
        else:
            for i in range(len(row)): # i is the column index
                if i in point_sequence_columns:
                    if (row_index <= 1 or row[i] != point_sequences[len(curves)-i].last_point()[1]): # Make sure the value is different from the last one
                        point_sequences[len(curves)-i].add_point((row_index-1)*args.x_multiplier, row[i])
                else:
                    curves[i].add_point((row_index-1)*args.x_multiplier, float(row[i]))
        row_index+=1

if profile and "curve_colors" in profile:
    for curve_name in profile["curve_colors"]:
        for curve in curves:
            if curve.name == curve_name:
                curve.color = profile["curve_colors"][curve_name]

        for pt_seq in point_sequences:
            if pt_seq.name == curve_name:
                pt_seq.color = profile["curve_colors"][curve_name]

clock = pygame.time.Clock()
while 1:
    deltaTime = clock.tick()

    pressed = pygame.key.get_pressed()

    for event in pygame.event.get():
        if event.type == pygame.QUIT or pressed[pygame.K_ESCAPE]:
            sys.exit()
        
        if event.type == pygame.KEYUP:
            if pressed[pygame.K_F12]:
                t = 1 if t<=0 else 0

        if event.type == pygame.MOUSEBUTTONDOWN:
            if pressed[pygame.K_x]:
                if event.button == 4 and x_scale > 0.5:
                    x_scale-=0.5
                elif event.button == 5:
                    x_scale+=0.5
            if pressed[pygame.K_y] or pressed[pygame.K_z]:
                if event.button == 4 and y_scale > 0.5:
                    y_scale-=0.5
                elif event.button == 5:
                    y_scale+=0.5

            elif not pressed[pygame.K_x]:
                if event.button == 4 and step_size > 1:
                    step_size-=1
                elif event.button == 5:
                    step_size+=1

    if pressed[pygame.K_RIGHT]:
        if t < 1:
            t+=0.0001*deltaTime
    elif pressed[pygame.K_LEFT]:
        if t > 0:
            t-=0.0001*deltaTime

    if t <= 0.25:
        t1 = t/0.25
        t2 = 0
    else:
        t1 = 1
        t2 = (t-0.25)/0.75

    screen.fill(background_col)
    for c in curves:
        if args.hide is None or c.name not in args.hide: 
            draw_curve(c, t2)
    for pt_seq in point_sequences:
        if args.hide is None or pt_seq.name not in args.hide: 
            draw_point_sequence(pt_seq)
    draw_axes(t1)
    pygame.display.flip()