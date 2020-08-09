#%%
from PIL import Image, ImageDraw, ImageFont
import numpy as np
from skimage.measure import label
from skimage.segmentation import flood_fill
from scipy.ndimage.measurements import center_of_mass
import pandas as pd
from tqdm import tqdm

#%%
#GENERATING PROVINCE MAP FROM OUTLINES
#map_outlines.bmp should be a white image with black outlines for provinces.
#Outlines can be any thickness but thinner generally better. Diagonals are OK.
im = Image.open("map_outlines.bmp")
p = np.array(im)
im.close()

#Just take first value of each pixel RGB, convert non-0 to 1
p = p[:,:,0]
p = np.where(p!=0,1,0)

#Apply an integer to each isolated island
areas = label(p)

#FILLING IN PROVINCE EDGES
#Loop through first/last column and fill in 0s
for i in range(areas.shape[0]):
    if areas[i,0] == 0:
        areas[i,0] = areas[i-1,0]
    if areas[i,areas.shape[1]-1] == 0:
        areas[i,areas.shape[1]-1] = areas[i-1,areas.shape[1]-1]
#Same for first/last row
for j in range(areas.shape[1]):
    if areas[0,j] == 0:
        areas[0,j] = areas[0,j-1]
    if areas[areas.shape[0]-1, j] == 0:
        areas[areas.shape[0]-1, j] = areas[areas.shape[0]-1, j-1]

#Loop through filling in 0s with adjacent colours until there are no 0s left
while (np.any(areas==0)):
    conditions = [
        areas != 0,
        np.roll(areas, 1, axis = 1) != 0,
        np.roll(areas, 1, axis = 0) != 0,
        np.roll(areas, -1, axis = 0) != 0,
        np.roll(areas, -1, axis = 1) != 0
    ]
    choices = [
        areas,
        np.roll(areas, 1, axis = 1),
        np.roll(areas, 1, axis = 0),
        np.roll(areas, -1, axis = 0),
        np.roll(areas, -1, axis = 1)
    ]
    areas = np.select(conditions, choices, default = areas)

#%%
#CONNECTING ISLANDS
#map_connectors.bmp should be a white image save for nodes that are contained within disjoint provinces 
#that are to be connected - in red; and 1px paths that connect these nodes - in blue.
im = Image.open("map_connectors.bmp")
p = np.array(im)

a = np.zeros((p.shape[0], p.shape[1])) # pylint: disable=unsubscriptable-object
#If b value is 0, assume it's a node (set to 1). If r value is 0, assume it's a path (set to 2). 
a = np.where(p[:,:,2] == 0, 1, a)
a = np.where(p[:,:,0] == 0, 2, a)

#This exports a image of the connector array, for checking 
def check_connectors():
    r = a * 100
    g = a % 2 * 100
    b = np.zeros(a.shape)
    rgb = np.stack([r,g,b], axis = 2)
    rgb = rgb.astype(np.uint8)
    output = Image.fromarray(rgb, "RGB")
    output.show()
#check_connectors()

nodes = np.argwhere(a == 1)
nodeset = set([tuple(x) for x in nodes])
paths = np.argwhere(a == 2)
pathset = set([tuple(x) for x in paths])

def find_node_pair(start, paths, nodes):
    #Finds paired node by moving along path.
    #Pass start as tuple, paths as set, nodes as set
    traversed = set()
    traversed.add(start)
    current = start
    found = False
    while not found:
        next = None
        pair = None
        checks = [(1,0), (-1,0), (0,1), (0,-1)]
        for (x,y) in checks:
            test = (current[0] + x, current[1] + y)
            if test in paths and test not in traversed:
                next = test
            elif test in nodes and test not in traversed:
                pair = test
                found = True
        if next is None and not found:
            print("Warning: could not find next path from " + str(current))
        else:
            traversed.add(current)
            current = next
    if pair is None:
        print("Warning: could not find pair for " + str(start))
    return pair

def fill_from_coord(areas, coord, value):
    areas = flood_fill(areas, coord, value)

def find_nodes_in_area(areas, node, nodes):
    #Finds any other nodes in the same area. Returns empty list otherwise
    value = areas[node]
    nodes_in_same_area = []
    for n in nodes:
        if n != node and areas[n] == value:
            nodes_in_same_area.append(n)
    return nodes_in_same_area

def get_matching_pair(element, pairs):
    for pair in pairs:
        if element in pair:
            return [x for x in pair if x != element][0]
    else:
        print("Warning: tried to get matching pair for {} but could not find one".format(element))
        
#Pair up sets in to list of pairs
pairs = []
paired = set()
for node in nodes:
    node = tuple(node)
    if node not in paired:
        pair = find_node_pair(node, pathset, nodeset)
        pairs.append([node, pair])
        paired.add(node)
        paired.add(pair)

#Fill same colour for each pair. Have to check multi-pair in same province as need to do at same time.
nodes_todo = set([(n[0],n[1]) for n in nodes])
while (len(nodes_todo) > 0):
    shared_nodes = []
    current_node = nodes_todo.pop()
    paired_node = get_matching_pair(current_node, pairs)
    nodes_todo.remove(paired_node)
    shared_nodes.append(current_node)
    shared_nodes.append(paired_node)

    #Find any other nodes that share regions
    nodes_to_check = set([current_node, paired_node])
    while (len(nodes_to_check) > 0):
        checking_node = nodes_to_check.pop()
        other_nodes_in_area = find_nodes_in_area(areas, checking_node, list(nodes_todo))
        for other_node in other_nodes_in_area:
            other_paired_node = get_matching_pair(other_node, pairs)
            nodes_todo.remove(other_node)
            nodes_todo.remove(other_paired_node)
            shared_nodes.append(other_node)
            shared_nodes.append(other_paired_node)
            nodes_to_check.add(other_node)
            nodes_to_check.add(other_paired_node)

    fill_value = areas[shared_nodes[0]]
    for shared_node in shared_nodes[1:]:
        current_value = areas[shared_node]
        areas[areas == current_value] = fill_value
    
#Match edges
left_values = np.unique(areas[:,0])
right_values = np.unique(areas[:,-1])
for left, right in zip(left_values, right_values):
    areas[areas == right] = left 


#%%
#REGIONS
#map_regions.bmp should be uniquely coloured areas, with white water and black untraversable
im = Image.open("map_regions.bmp")
p = np.array(im)
im.close()

def rgb_to_int(r,g,b):
    return r * 0x10000 + g * 0x100 + b

def int_to_rgb(i):
    r = round(i/0x10000)
    g = round((i-r*0x10000)/0x100)
    b = i-r*0x10000-g*0x100
    return r,g,b

regions = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])
#For later, keep track of the water and untraversable tiles
water_mask = (regions == rgb_to_int(255,255,255))
untraversable_mask = (regions == 0)

#Rename regions to consecutive integers
#First need to ensure no overlap between current integers and the ones we will rename to
regions += len(np.unique(regions))
#Now revalue each old region id integer to a new gradually ascending number
#Sort the flattened index array so the new ids ascend left to right, top to bottom
old_ids, old_index = np.unique(regions, return_index = True)
old_ids = old_ids[np.argsort(old_index)]
for new_id, old_id in enumerate(old_ids):
    regions[regions == old_id] = new_id

#We specially reserve 0 for untraversable, and 1 for water. So need to swap accordingly
old_untraversable_value = regions[untraversable_mask][0]
regions[regions == 0] = old_untraversable_value
regions[untraversable_mask] = 0

old_water_value = regions[water_mask][0]
regions[regions == 1] = old_water_value
regions[water_mask] = 1

#Generate region lookup dicts
print("Generating areas_in_region")
areas_in_region = {}
for region in tqdm(np.unique(regions)):
    areas_in_region[region] = np.unique(areas[regions == region]).tolist()

print("Generating region_lookup")
region_lookup = {}
for area in tqdm(np.unique(areas)):
    value = np.unique(regions[areas == area])
    if len(value) > 1:
        print("Warning! Area {} belongs to more than one region: {}".format(area, value))
    region_lookup[area] = value[0]


#%%
#Generate the map_info Dataframe
info = pd.DataFrame(index = np.unique(areas))

info["Region"] = info.index.map(region_lookup)

#%%
#CAPITOLS
#map_capitols.bmp should be black other than capitol provinces, which are white
im = Image.open("map_capitols.bmp")
p = np.array(im)
im.close()

#Just take the r value, to simplify
p = p[:,:,0]
capitol_mask = (p != 0)

#Check we have at least one for each region
region_ids_with_capitol = np.unique(regions[capitol_mask])
region_ids = np.unique(regions)
if region_ids_with_capitol != region_ids:
    region_ids_without_capitol = region_ids[~np.isin(region_ids,region_ids_with_capitol)]
    print("Warning! Not every region has a capitol. Missing for: " + str(region_ids_without_capitol))

capitols = areas[capitol_mask]
capitol_ids = np.unique(capitols)
info["Capitol"] = np.where(info.index.isin(capitol_ids), True, False)

#Check we have only one for each region
pd_capitols = info[info["Capitol"]]
duplicate_capitols = pd_capitols[pd_capitols.duplicated(["Region"], keep=False)]
if len(duplicate_capitols) > 0:
    print("Warning! Following areas are detected as capitols but share a region: ")
    print(duplicate_capitols)
else:
    print("No doubling-up of capitols in regions detected. OK.")

# %%
#PLOTTING

def draw_label_numbers(img, labels, selected_labels):
    print("Drawing labelled numbers...")
    #Draws a number over the center of each labelled area selected
    for label in tqdm(selected_labels):
        #Scikit center_of_mass gives row then column so need to reverse for x and y in plot
        location = center_of_mass(labels==label)[::-1]
        draw_text(img, str(label)
        , location)
    return img

def draw_text(img, text, location, size = 35, fill = (0,0,0)):
    #Adjusts to center text on location
    font = ImageFont.truetype("arial.ttf", size)
    img_draw = ImageDraw.Draw(img)
    w,h = img_draw.textsize(text, font = font)
    img_draw.text((location[0]-w/2, location[1]-h/2), text, font = font, fill = fill)

def generate_binary_image(data, draw = True):
    return generate_image(data*255, data*255, data*255, draw)

def generate_image(r, g, b, draw = True):
    rgb = np.stack([r,g,b], axis = 2)
    rgb = rgb.astype(np.uint8)
    img = Image.fromarray(rgb, "RGB")
    if draw:
        draw_image(img)
    return img, rgb

def draw_image(img, show = True, save = False):
    if show:
        img.show()
    if save:
        img.save(save)

def draw_areas(areas):
    #Get some pseudo-random colours so that nearby tiles are not similarly coloured
    r = areas % 256
    g = (10*areas+50) % 203
    b = (20*areas+100) % 193
    #Check the rgb generation 
    check = r*1000000 + g * 1000 + b
    if len(np.unique(check)) != len(np.unique(areas)):
        print("Warning: RGB check failed! Number of labels do not match number of colours")

    img, rgb = generate_image(r,g,b, draw = False)
    # img = draw_province_numbers(img, areas, [399, 425, 618, 639, 703, 836, 995, 1026, 1102, 1111, 1119, 1167, 1353, 1366])
    draw_image(img, save = "output.bmp")
    return img, rgb

def draw_regions(regions):
    img = Image.open("map_regions.bmp")
    img = draw_label_numbers(img, regions, np.unique(regions))
    draw_image(img, save = "map_regions_numbered.bmp")
    img.close()

# draw_areas(areas)
# draw_regions(regions)
generate_binary_image(areas == 347)

# %%
def check_province_sizes(labels, threshold = 100):
    failures = []
    for label in tqdm(np.unique(labels)):
        if np.count_nonzero(labels == label) < threshold:
            failures.append(label)
    print("Checks completed. Found {} failures".format(len(failures)))
    return failures

check_province_sizes(areas)

# %%
