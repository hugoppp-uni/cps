#

## Install runtime:
- https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-2210

## Install IDE (e.g. Rider)
- https://www.jetbrains.com/rider/download/

## Open solution
- Open the `.sln` file in the IDE and select  *Open as Project*

## Generate map
Go to [https://overpass-turbo.eu/](https://overpass-turbo.eu/) Execute the follwing code. The output will be limited to the visible area.
```
[out:json]; 
(
  way[highway=motorway]({{bbox}});
  way[highway=motorway_link]({{bbox}});
  way[highway=trunk]({{bbox}});
  way[highway=trunk_link]({{bbox}});
  way[highway=primary]({{bbox}});
  way[highway=secondary]({{bbox}});
  way[highway=tertiary]({{bbox}});
  way[highway=unclassified]({{bbox}});
  way[highway=residential]({{bbox}});
);
(._;>;);
out;
``` 
