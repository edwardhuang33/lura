#%%
from PIL import Image, ImageDraw, ImageFont
import numpy as np
from skimage.measure import label
from skimage.segmentation import flood_fill
from scipy.ndimage.measurements import center_of_mass
import pandas as pd
from tqdm import tqdm
import pickle
import tripy
import json

def rgb_to_int(r,g,b):
    return r * 0x10000 + g * 0x100 + b

def int_to_rgb(i):
    r = np.round(i/0x10000)
    g = np.round((i-r*0x10000)/0x100)
    b = i-r*0x10000-g*0x100
    return r,g,b


#%%
#READING AREA FILE
#If there already is a fully-formed area bmp with unique colours, no need to do steps above.
#This below part works exactly like the REGIONS section below, so refer there for description
im = Image.open("map_areas.bmp")
p = np.array(im)
im.close()

print("Processing map_areas.bmp...")
areas = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])
# areas += len(np.unique(areas))

# old_ids, old_index = np.unique(areas, return_index = True)
# old_ids = old_ids[np.argsort(old_index)]
# for new_id, old_id in enumerate(old_ids):
#     areas[areas == old_id] = new_id

#Currently still use map_connectors.bmp to check contiguousness. Should maybe redo to work natively without other input
print("Checking contiguousness...")
noncontiguous_areas = [x for x in np.unique(areas[:,0])]
noncontiguous_areas += [x for x in np.unique(areas[0,:]) if x not in noncontiguous_areas]
noncontiguous_areas += [x for x in np.unique(areas[:,-1]) if x not in noncontiguous_areas]
noncontiguous_areas += [x for x in np.unique(areas[-1,:]) if x not in noncontiguous_areas]

im = Image.open("map_connectors.bmp")
connectors = np.array(im)
im.close()

connectors = rgb_to_int(connectors[:,:,0], connectors[:,:,1], connectors[:,:,2])
red = rgb_to_int(255,0,0)
for node in np.argwhere(connectors == red):
    area_id = areas[tuple(node)]
    if area_id not in noncontiguous_areas:
        noncontiguous_areas.append(area_id)

#%%
#REGIONS
#map_regions.bmp should be uniquely coloured areas, with white water and black untraversable
im = Image.open("map_regions.bmp")
p = np.array(im)
im.close()

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
#CONTINENTS
#map_regions.bmp should be uniquely coloured areas, with white for all non-continents
im = Image.open("map_continents.bmp")
p = np.array(im)
im.close()

continents = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])

#Generate continent lookup dicts
print("Generating areas_in_continent")
areas_in_continent = {}
for continent in tqdm(np.unique(continents)):
    areas_in_continent[continent] = np.unique(areas[continents == continent]).tolist()

print("Generating continent_lookup")
continent_lookup = {}
for area in tqdm(np.unique(areas)):
    value = np.unique(continents[areas == area])
    if len(value) > 1:
        print("Warning! Area {} belongs to more than one continent: {}".format(area, value))
    continent_lookup[area] = value[0]

#%%
#TERRAIN
im = Image.open("map_terrain.bmp")
p = np.array(im)
im.close()

terrains = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])

print("Generating areas_in_terrain")
areas_in_terrain = {}
for terrain in tqdm(np.unique(terrains)):
    areas_in_terrain[terrain] = np.unique(areas[terrains == terrain]).tolist()

print("Generating terrain_lookup")
terrain_lookup = {}
for area in tqdm(np.unique(areas)):
    value = np.unique(terrains[areas == area])
    if len(value) > 1:
        print("Warning! Area {} belongs to more than one terrain: {}".format(area, value))
    terrain_lookup[area] = value[0]

#%%
#MODIFIERS
im = Image.open("map_modifiers.bmp")
p = np.array(im)
im.close()

modifiers = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])

print("Generating areas_in_modifier")
areas_in_modifier = {}
for modifier in tqdm(np.unique(modifiers)):
    areas_in_modifier[modifier] = np.unique(areas[modifiers == modifier]).tolist()

print("Generating modifier_lookup")
modifier_lookup = {}
for area in tqdm(np.unique(areas)):
    value = np.unique(modifiers[areas == area])
    if len(value) > 1:
        print("Warning! Area {} belongs to more than one modifier: {}".format(area, value))
    modifier_lookup[area] = value[0]

#%%
#OWNERS
im = Image.open("map_starts_1.bmp")
p = np.array(im)
im.close()

owners = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])
print("Generating areas_in_owner")
areas_in_owner = {}
for owner in tqdm(np.unique(owners)):
    areas_in_owner[owner] = np.unique(areas[owners == owner]).tolist()

print("Generating owner_lookup")
owner_lookup = {}
for area in tqdm(np.unique(areas)):
    value = np.unique(owners[areas == area])
    if len(value) > 1:
        print("Warning! Area {} belongs to more than one owner: {}".format(area, value))
    owner_lookup[area] = value[0]
    

#%%
#Generate the map_info Dataframe
info = pd.DataFrame(index = np.unique(areas))
info["ID"] = info.index.astype(str)
info["name"] = "Area #" + info.index.astype(str)

info["region"] = info.index.map(region_lookup)
input_regions = pd.read_excel("map_input.xlsx", sheet_name="Regions")
info["regionName"] = info["region"].map(input_regions.set_index("ID")["Name"])

info["contiguous"] = ~info.index.isin(noncontiguous_areas)

info["continent"] = info.index.map(continent_lookup)
input_continents = pd.read_excel("map_input.xlsx", sheet_name="Continents")
info["continentName"] = info["continent"].map(input_continents.set_index("ID")["Name"])

info["terrain"] = info.index.map(terrain_lookup)
input_terrain = pd.read_excel("map_input.xlsx", sheet_name="Terrains")
info["terrainName"] = info["terrain"].map(input_terrain.set_index("ID")["Name"])

info["modifier"] = info.index.map(modifier_lookup)
input_modifiers = pd.read_excel("map_input.xlsx", sheet_name="Modifiers")
info["modifierName"] = info["modifier"].map(input_modifiers.set_index("ID")["Name"])

info["owner"] = info.index.map(owner_lookup)
input_owners = pd.read_excel("map_input.xlsx", sheet_name="Owners")
info["ownerName"] = info["owner"].map(input_owners.set_index("ID")["Name"])

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
info["capitol"] = info.index.isin(capitol_ids)

#Check we have only one for each region
pd_capitols = info[info["capitol"]]
duplicate_capitols = pd_capitols[pd_capitols.duplicated(["region"], keep=False)]
if len(duplicate_capitols) > 0:
    print("Warning! Following areas are detected as capitols but share a region: ")
    print(duplicate_capitols)
else:
    print("No doubling-up of capitols in regions detected. OK.")

#%%
#COORDS
h, w = areas.shape
info["coords"] = np.nan
info["coords"] = info["coords"].astype("object")
for i in tqdm(info.index):
    arr = np.argwhere(areas == i)
    #Convert so reading xy from bottom left for Unity
    arr = np.fliplr(arr)
    arr = arr * [-1, 1]
    arr = [0, h-1] - arr
    #Then convert to a flattened 1d for Texture2D SetPixels32 use 
    flattened = arr[:,0] + arr[:,1] * w
    info.at[i, "coords"] = flattened

#%%
#CENTRES
im = Image.open("map_centres.bmp")
p = np.array(im)
im.close()

centres = rgb_to_int(p[:,:,0], p[:,:,1], p[:,:,2])
white = rgb_to_int(255,255,255)

manual_centres = np.argwhere(centres == white)
info["centre"] = np.nan
info["centre"] = info["centre"].astype("object")
print("Setting manual centres...")
h = areas.shape[0]
for centre in tqdm(manual_centres):
    area = areas[tuple(centre)]
    
    centre = [centre[1], h - centre[0] - 1] # reverse for xy and set origin to bottom left (for Unity)
    info.at[area, "centre"] = tuple(centre)

print("Setting automated centres")
for area in tqdm(info[info["centre"].isna()].index):
    centre = center_of_mass(areas == area)
    centre = [int(centre[1]), int(h - centre[0] - 1)] # reverse for xy and set origin to bottom left (for Unity)
    info.at[area, "centre"] = tuple(centre) #reverse and round


#%%
#NEIGHBOURS
info["neighbours"] = np.nan
info["neighbours"] = info["neighbours"].astype("object")
for area in tqdm(np.unique(areas)):
    neighbours = set()
    mask = areas == area
    neighbours.update(list(np.unique(np.roll(areas, 1, axis = 1)[mask])))
    neighbours.update(list(np.unique(np.roll(areas, -1, axis = 1)[mask])))
    neighbours.update(list(np.unique(np.roll(areas, 1, axis = 0)[mask])))
    neighbours.update(list(np.unique(np.roll(areas, -1, axis = 0)[mask])))
    neighbours = list(set([x for x in neighbours if x != area]))
    info.at[area, "neighbours"] = neighbours

top_row_areas = np.unique(areas[0,:])
bottom_row_areas = np.unique(areas[-1,:])

print("Cleaning up top and bottom rows")
for area in top_row_areas:
    info.at[area, "neighbours"] = [x for x in info.at[area, "neighbours"] if x not in bottom_row_areas]
for area in bottom_row_areas:
    info.at[area, "neighbours"] = [x for x in info.at[area, "neighbours"] if x not in top_row_areas]



#%%
#WRITE INFO DATAFRAME TO FILE
print("Writing info DataFrame...")
with open("map_info.pkl", "wb") as file:
    pickle.dump(info, file)

info.to_json("map_info.json")

print("Adding extra JSON info...")
with open("map_info.json") as f:
    data = json.load(f)

json_areas_in_region = {}
for key in areas_in_region.keys():
    json_areas_in_region[str(key)] = [int(x) for x in areas_in_region[key]]
json_areas_in_continent = {}
for key in areas_in_continent.keys():
    json_areas_in_continent[str(key)] = [int(x) for x in areas_in_continent[key]]
json_areas_in_terrain = {}
for key in areas_in_terrain.keys():
    json_areas_in_terrain[str(key)] = [int(x) for x in areas_in_terrain[key]]
json_areas_in_modifier = {}
for key in areas_in_modifier.keys():
    json_areas_in_modifier[str(key)] = [int(x) for x in areas_in_modifier[key]]
json_areas_in_owner = {}
for key in areas_in_owner.keys():
    json_areas_in_owner[str(key)] = [int(x) for x in areas_in_owner[key]]

data.update({"areasInRegion": json_areas_in_region})
data.update({"areasInContinent": json_areas_in_continent})
data.update({"areasInTerrain": json_areas_in_terrain})
data.update({"areasInModifier": json_areas_in_modifier})
data.update({"areasInOwner": json_areas_in_owner})

json_region_info = input_regions.set_index(input_regions["ID"].astype(str))
json_region_info = json_region_info["Name"].to_dict()
json_continent_info = input_continents.set_index(input_continents["ID"].astype(str))
json_continent_info = json_continent_info["Name"].to_dict()
json_terrain_info = input_terrain.set_index(input_terrain["ID"].astype(str))
json_terrain_info = json_terrain_info["Name"].to_dict()
json_modifier_info = input_modifiers.set_index(input_modifiers["ID"].astype(str))
json_modifier_info = json_modifier_info["Name"].to_dict()
json_owner_info = input_owners.set_index(input_owners["ID"].astype(str))
json_owner_info = json_owner_info["Name"].to_dict()

data.update({"regionInfo": json_region_info})
data.update({"continentInfo": json_continent_info})
data.update({"terrainInfo": json_terrain_info})
data.update({"modifierInfo": json_modifier_info})
data.update({"ownerInfo": json_owner_info})


with open("map_info.json", "w") as f:
    json.dump(data, f)

with open("map_info_extra.pkl", "wb") as file:
    pickle.dump((json_areas_in_region,json_areas_in_continent,json_areas_in_terrain,json_areas_in_modifier, json_areas_in_owner,
                json_region_info,json_continent_info,json_terrain_info,json_modifier_info, json_owner_info), file)


#%%
with open("map_info.pkl", "rb") as file:
    info = pickle.load(file)

with open("map_info_extra.pkl", "rb") as file:
    (json_areas_in_region,json_areas_in_continent,json_areas_in_terrain,json_areas_in_modifier, json_areas_in_owner,
    json_region_info,json_continent_info,json_terrain_info,json_modifier_info, json_owner_info) = pickle.load(file)











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

#List of areas that are noncontiguous, generated by this cell.
noncontiguous_areas = []

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
    noncontiguous_areas.append(fill_value)
    
#Match edges
left_values = np.unique(areas[:,0])
right_values = np.unique(areas[:,-1])
for left, right in zip(left_values, right_values):
    areas[areas == right] = left
    noncontiguous_areas.append(left)




#%%
#EDGES
#If adjacent tile is different area, mark as edge. 
edge_mask = ((np.roll(areas, 1, axis = 1) != areas) | 
            (np.roll(areas, 1, axis = 0) != areas) |
            (np.roll(areas, -1, axis = 0) != areas) |
            (np.roll(areas, -1, axis = 1) != areas))

#Also mark every border coord as an edge
edge_mask[0,:] = True
edge_mask[:,0] = True
edge_mask[-1,:] = True
edge_mask[:,-1] = True

def get_edge_order(edges):
    #edges should be an iterable of numpy 1d size 2 arrays
    #Move around the edge tiles (diagonals OK) until you reach the start again.
    start = edges[0]
    order = np.array([start])
    current = start
    directions = [(1,0),(0,1),(-1,0),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]
    directions = [np.array(x) for x in directions]
    reversing = 0
    for i in range(10000):
        next_edge_found = False
        for direction in directions:
            candidate = current + direction
            if np.any(np.equal(edges, candidate).all(1)) and not np.any(np.equal(order, candidate).all(1)):
                current = candidate
                order = np.r_[order,[candidate]]
                reversing = 0
                next_edge_found = True
                break
        #If couldn't find another direction e.g. could be single pixel outreach. 
        #So move back to previous and continue
        if not next_edge_found:
            reversing -= 1
            current = order[reversing-1]
        if np.all(start == current):
            if len(order) != len(edges):
                area_id = areas[tuple(start)]
                print("Warning! Stopping but {0} edge pixels unaccounted for in area {1}".format(len(edges) - len(order), area_id))
                #Note that this will currently throw errors for areas that totally encompass a small (sub)area.
            break

    return order
    
#Get ordered list of edge coords for each area. If non-contiguous, do for each contiguous subarea. Stores as list of nx2 np arrays
info["edges"] = np.nan
edges_ordered = {}
for i in tqdm(np.unique(areas)):
    edges_ordered[i] = []
    if i not in noncontiguous_areas:
        edges = np.argwhere(np.logical_and(areas == i, edge_mask))
        order = get_edge_order(edges)
        edges_ordered[i].append(order)
    else:
        subareas = label(areas == i)
        for subarea_id in np.unique(subareas):
            if subarea_id != 0:
                edges = np.argwhere(np.logical_and(subareas == subarea_id, edge_mask))
                order = get_edge_order(edges)
                edges_ordered[i].append(order)
    info["edges"][i] = edges_ordered[i]
#Note that this will currently throw errors for areas that totally encompass a small (sub)area.




#%%
#MESH GENERATION
info["meshEdges"] = np.nan
info["meshEdges"] = info["meshEdges"].astype("object")
info["colliderEdges"] = np.nan
info["colliderEdges"] = info["colliderEdges"].astype("object")
for i in info.index:
    #Generate an abbreviated version of the full edge list
    mesh_edge_list = []
    collider_edge_list = []
    for edge_list in info["edges"][i]:
        #Change the last number in the line below to adjust how many vertices to generate
        step = max(round(len(edge_list)/60), 1) 
        mesh_edges = edge_list[::step]
        
        step = max(round(len(edge_list)/15), 1) 
        collider_edges = edge_list[::step]
        
        #Generation comes in yx format so need to swap round for Unity
        mesh_edge_list.append([(c[1],c[0]) for c in mesh_edges])
        collider_edge_list.append([(c[1],c[0]) for c in collider_edges])
    info.at[i, "meshEdges"] = mesh_edge_list
    info.at[i, "colliderEdges"] = collider_edge_list

def get_clockwise_triangle_index(trio, all_coord):
    #First check if clockwise, and reverse if not
    #Test on [0,1] [1,1] [1,0] returns -1 so clockwise is positive (recall origin is top-left)
    #reference: https://math.stackexchange.com/questions/1324179/
    stack = np.vstack([np.array(c) for c in trio])
    M = np.c_[stack, np.ones(3)]
    if np.linalg.det(M) < 0:
        print("Found counter-clockwise trio: " + str(trio))
        trio = trio[::-1]
    elif np.linalg.det(M) == 0:
        print("Found straight-line trio: " + str(trio))

    #Then return the index of the coord within the wider list (as per Unity mesh requirements)
    #This isn't terribly efficient but in our scope it works fast enough
    triangle = []
    for coord in trio:
        index = np.where(np.all(np.array(coord) == all_coord, axis=1))[0][0]
        triangle.append(index)
    return triangle

#Calculate triangle array from the ordered mesh edges
#Once for rendered area, once (simplified) for colliders
info["meshTriangles"] = np.nan
info["meshTriangles"] = info["meshTriangles"].astype("object")
info["colliderTriangles"] = np.nan
info["colliderTriangles"] = info["colliderTriangles"].astype("object")

for i in tqdm(info.index):
    for edge_type in ["meshEdges", "colliderEdges"]: 
        mesh_edges = info.at[i, edge_type]
        triangulated = []
        for perimeter in mesh_edges:
            #We use tripy's earclip triangulation algorithm. Yes I'm lazy, sue me.
            coord_triangles = tripy.earclip(perimeter)
            #This gives coordinates, we only need the index. Also need to check its clockwise
            #Custom function to achieve this
            triangles = []
            for trio in coord_triangles:
                triangle = get_clockwise_triangle_index(trio, perimeter)
                triangles.append(triangle)
            triangulated.append(triangles)
        if edge_type == "meshEdges":
            info.at[i, "meshTriangles"] = triangulated
        elif edge_type == "colliderEdges":
            info.at[i, "colliderTriangles"] = triangulated


 
# %%
#PLOTTING

def draw_label_numbers(img, labels, selected_labels):
    print("Drawing labelled numbers...")
    #Draws a number over the center of each labelled area selected
    for label in tqdm(selected_labels):
        #Scikit center_of_mass gives row then column so need to reverse for x and y in plot
        location = center_of_mass(labels==label)[::-1]
        draw_text(img, str(label), location)
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

def draw_image(img, show = True, save = "output.bmp"):
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
# generate_binary_image(areas == 347)

# %%
def check_area_sizes(labels, threshold = 100):
    failures = []
    for label in tqdm(np.unique(labels)):
        if np.count_nonzero(labels == label) < threshold:
            failures.append(label)
    print("Checks completed. Found {} failures".format(len(failures)))
    return failures

check_area_sizes(areas)

# %%
